using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mochi.DataTable
{
    /// <summary>
    /// JSON 装载管线:文本、表头识别、反序列化、表内结构校验、注册五个步骤的单一入口。
    /// 只接收文本与文件名字符串,不做任何 I/O,资源获取由调用方完成;文件名仅作为错误定位标签使用。
    /// 表头与代码注册表交叉配对:表头 table 须与 <see cref="TableDefinition{TRow}.TableName"/> 一致,
    /// 表头 rowType 须与装载泛型行类型的完整类名一致,任一不符即装载失败,因为文件与代码指向同一张表是注册成立的前提。
    /// 行反序列化启用演化容错:字段缺失、类型不匹配或字段改名取默认值或旧名回退,装载不因单字段失败;
    /// 结构校验(顶层形状、行对象、id 非空唯一)不归容错兜底,仍整体拒绝。
    /// 单表内收集全部错误后整体拒绝;数据非法时返回失败结果,不抛异常、不注册。
    /// 表名已被注册时沿用 RegisterDataTable 的抛错语义(fail loud),因为重复装载属于调用顺序错误,不是数据错误。
    /// </summary>
    public static class JsonTableLoader
    {
        /// <summary>行反序列化器;字段级容错语义见 <see cref="TableJsonSerialization.CreateEvolutionSettings"/>。</summary>
        private static readonly JsonSerializer RowSerializer = CreateRowSerializer();

        private static JsonSerializer CreateRowSerializer()
        {
            return JsonSerializer.Create(TableJsonSerialization.CreateEvolutionSettings());
        }

        /// <summary>
        /// 装载一张表:解析顶层对象与表头、将表名与行类型与代码注册表交叉配对、逐条校验表内结构,
        /// 全部通过时构造封口表并以表头 table 为名注册;行字段反序列化启用演化容错。
        /// </summary>
        /// <param name="definition">代码注册表声明;TableName 参与表头交叉配对。</param>
        /// <param name="fileName">文件名,仅用于错误定位。</param>
        /// <param name="json">表文件的 JSON 文本,顶层必须是 {"$table":{...},"rows":[...]} 表对象。</param>
        /// <exception cref="ArgumentNullException">definition 或 json 为 null。</exception>
        /// <exception cref="ArgumentException">fileName 为 null 或空。</exception>
        /// <exception cref="InvalidOperationException">数据合法但表名已被注册,由 RegisterDataTable 抛出。</exception>
        public static TableLoadResult<TRow> Load<TRow>(
            TableDefinition<TRow> definition,
            string fileName,
            string json)
            where TRow : TableRow
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("文件名不能为空。", nameof(fileName));
            }
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var errors = new List<TableLoadError>();
            JToken root;
            try
            {
                root = JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                errors.Add(new TableLoadError(fileName, null, null, "JSON 语法错误: " + ex.Message));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            if (root.Type != JTokenType.Object)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null,
                    "顶层不是表对象,实际类型为 " + root.Type + ",缺少 " + TableHeader.FieldName + " 表头。"));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            if (!TableHeader.TryParse((JObject)root, out TableHeader header, out string headerError))
            {
                errors.Add(new TableLoadError(fileName, null, null, headerError));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            if (header.Table != definition.TableName)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null,
                    "表头表名 " + header.Table + " 与代码注册表声明的 " + definition.TableName + " 不符。"));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            if (header.RowType != typeof(TRow).FullName)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null,
                    "表头行类型 " + header.RowType + " 与装载行类型 " + typeof(TRow).FullName + " 不符。"));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            JToken rowsToken = root[TableHeader.RowsFieldName];
            if (rowsToken == null || rowsToken.Type != JTokenType.Array)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null,
                    "缺少 " + TableHeader.RowsFieldName + " 行数组,实际类型为 " +
                    (rowsToken == null ? "缺失" : rowsToken.Type.ToString()) + "。"));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            var rows = new List<TRow>();
            var keyOrdinals = new Dictionary<string, int>();
            int ordinal = 0;
            foreach (JToken entry in rowsToken)
            {
                ordinal++;
                LoadEntry(fileName, entry, ordinal, rows, keyOrdinals, errors);
            }

            if (errors.Count > 0)
            {
                return new TableLoadResult<TRow>(false, errors, null);
            }

            ValidatedTable<TRow> table = ValidatedTable<TRow>.BuildFromRows(rows);
            DataTableManager.Instance.RegisterDataTable(header.Table, table);
            return new TableLoadResult<TRow>(true, errors, table);
        }

        private static void LoadEntry<TRow>(
            string fileName,
            JToken entry,
            int ordinal,
            List<TRow> rows,
            Dictionary<string, int> keyOrdinals,
            List<TableLoadError> errors)
            where TRow : TableRow
        {
            if (entry.Type != JTokenType.Object)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null, $"第 {ordinal} 个条目不是 JSON 对象,实际类型为 {entry.Type}。"));
                return;
            }

            var entryObject = (JObject)entry;
            string keyFieldName = TableRow.IdFieldName;
            string entryId = ReadEntryId(entryObject, keyFieldName);

            if (!IsFieldPresent(entryObject, keyFieldName))
            {
                errors.Add(new TableLoadError(
                    fileName, entryId, keyFieldName,
                    $"第 {ordinal} 个条目缺少键字段 {keyFieldName}。"));
            }

            TRow row;
            try
            {
                row = entry.ToObject<TRow>(RowSerializer);
            }
            catch (JsonException ex)
            {
                //演化容错已在设置内吞掉成员级错误,能到这里的是它不覆盖的结构级异常
                errors.Add(new TableLoadError(
                    fileName, entryId, null, $"第 {ordinal} 个条目反序列化失败: {ex.Message}"));
                return;
            }

            string key = row.id;
            if (string.IsNullOrEmpty(key))
            {
                errors.Add(new TableLoadError(
                    fileName, null, keyFieldName,
                    $"第 {ordinal} 个条目的键字段 {keyFieldName} 无有效值。"));
                return;
            }
            if (keyOrdinals.TryGetValue(key, out int firstOrdinal))
            {
                errors.Add(new TableLoadError(
                    fileName, key, keyFieldName,
                    $"第 {ordinal} 个条目与第 {firstOrdinal} 个条目键重复: {key}。"));
                return;
            }

            keyOrdinals.Add(key, ordinal);
            rows.Add(row);
        }

        private static string ReadEntryId(JObject entry, string keyFieldName)
        {
            JToken idToken = entry[keyFieldName];
            if (idToken == null || idToken.Type == JTokenType.Null)
            {
                return null;
            }
            return idToken.ToString();
        }

        private static bool IsFieldPresent(JObject entry, string fieldName)
        {
            JToken token = entry[fieldName];
            return token != null && token.Type != JTokenType.Null;
        }
    }
}
