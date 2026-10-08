using System;
using Newtonsoft.Json.Linq;

namespace Mochi.DataTable
{
    /// <summary>
    /// 数据表文件的自描述表头,即顶层对象的 "$table" 字段。文件整体形状为
    /// {"$table":{table,rowType,formatVersion},"rows":[...]};
    /// 表名与行类型以表头为识别来源,装载管线与编辑器浏览器共用本类型,避免两处各自解析漂移。
    /// </summary>
    public sealed class TableHeader
    {
        /// <summary>表头在顶层对象中的字段名。</summary>
        public const string FieldName = "$table";

        /// <summary>行数组在顶层对象中的字段名。</summary>
        public const string RowsFieldName = "rows";

        /// <summary>当前表头格式版本;只随表头结构本身的演进递增,不随行数据结构变化。</summary>
        public const int CurrentFormatVersion = 1;

        /// <summary>表名,即 DataTableManager 中的注册名。</summary>
        public string Table { get; }

        /// <summary>行类型的完整类名,是文件指向代码的唯一锚点;重命名即失配,由消费方显式处理。</summary>
        public string RowType { get; }

        /// <summary>表头格式版本。</summary>
        public int FormatVersion { get; }

        /// <param name="table">表名。</param>
        /// <param name="rowType">行类型完整类名。</param>
        /// <param name="formatVersion">表头格式版本。</param>
        public TableHeader(string table, string rowType, int formatVersion)
        {
            Table = table;
            RowType = rowType;
            FormatVersion = formatVersion;
        }

        /// <summary>
        /// 从表文件顶层对象解析表头。字段缺失或类型不符即整体失败,不做部分容错,
        /// 因为半份表头无法支撑表名与行类型的识别。失败原因经 <paramref name="error"/> 返回。
        /// </summary>
        public static bool TryParse(JObject root, out TableHeader header, out string error)
        {
            header = null;
            if (root == null)
            {
                error = "顶层不是 JSON 对象,缺少 " + FieldName + " 表头。";
                return false;
            }

            JToken headerToken = root[FieldName];
            if (headerToken == null || headerToken.Type != JTokenType.Object)
            {
                error = "缺少 " + FieldName + " 表头对象。";
                return false;
            }

            var headerObject = (JObject)headerToken;
            JToken tableToken = headerObject["table"];
            if (tableToken == null || tableToken.Type != JTokenType.String ||
                string.IsNullOrEmpty((string)tableToken))
            {
                error = FieldName + ".table 必须是非空字符串。";
                return false;
            }
            JToken rowTypeToken = headerObject["rowType"];
            if (rowTypeToken == null || rowTypeToken.Type != JTokenType.String ||
                string.IsNullOrEmpty((string)rowTypeToken))
            {
                error = FieldName + ".rowType 必须是非空字符串。";
                return false;
            }
            JToken versionToken = headerObject["formatVersion"];
            if (versionToken == null || versionToken.Type != JTokenType.Integer)
            {
                error = FieldName + ".formatVersion 必须是整数。";
                return false;
            }

            header = new TableHeader(
                (string)tableToken, (string)rowTypeToken, (int)versionToken);
            error = null;
            return true;
        }

        /// <summary>构造写入文件用的表头 JSON 对象。</summary>
        public JObject ToJson()
        {
            var header = new JObject
            {
                ["table"] = Table,
                ["rowType"] = RowType,
                ["formatVersion"] = FormatVersion,
            };
            return header;
        }
    }
}
