using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mochi.DataTable
{
    /// <summary>
    /// JSON 装载管线:文本、反序列化、表内结构校验、注册四个步骤的单一入口。
    /// 只接收文本与文件名字符串,不做任何 I/O,资源获取由调用方完成;文件名仅作为错误定位标签使用。
    /// 单表内收集全部错误后整体拒绝;数据非法时返回失败结果,不抛异常、不注册。
    /// 表名已被注册时沿用 RegisterDataTable 的抛错语义(fail loud),因为重复装载属于调用顺序错误,不是数据错误。
    /// </summary>
    public static class JsonTableLoader
    {
        /// <summary>
        /// 装载一张表:解析为行数组、逐条校验表内结构,全部通过时构造封口表并注册。
        /// 行键取每行的 <see cref="TableRow.id"/>。
        /// </summary>
        /// <param name="definition">表声明。</param>
        /// <param name="fileName">文件名,仅用于错误定位。</param>
        /// <param name="json">表文件的 JSON 文本,顶层必须是行对象数组。</param>
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

            if (root.Type != JTokenType.Array)
            {
                errors.Add(new TableLoadError(
                    fileName, null, null, "顶层不是行对象数组,实际类型为 " + root.Type + "。"));
                return new TableLoadResult<TRow>(false, errors, null);
            }

            var rows = new List<TRow>();
            var keyOrdinals = new Dictionary<string, int>();
            int ordinal = 0;
            foreach (JToken entry in root)
            {
                ordinal++;
                LoadEntry(definition, fileName, entry, ordinal, rows, keyOrdinals, errors);
            }

            if (errors.Count > 0)
            {
                return new TableLoadResult<TRow>(false, errors, null);
            }

            ValidatedTable<TRow> table = ValidatedTable<TRow>.BuildFromRows(rows);
            DataTableManager.Instance.RegisterDataTable(definition.TableName, table);
            return new TableLoadResult<TRow>(true, errors, table);
        }

        private static void LoadEntry<TRow>(
            TableDefinition<TRow> definition,
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
            foreach (string field in definition.RequiredFields)
            {
                if (!IsFieldPresent(entryObject, field))
                {
                    errors.Add(new TableLoadError(
                        fileName, entryId, field,
                        $"条目 {entryId ?? ("第 " + ordinal + " 个")} 缺少必填字段 {field}。"));
                }
            }

            TRow row;
            try
            {
                row = entry.ToObject<TRow>();
            }
            catch (JsonException ex)
            {
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
