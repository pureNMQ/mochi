namespace Mochi.DataTable
{
    /// <summary>
    /// 一条装载错误定位记录。
    /// 文件级错误(如 JSON 语法错)的条目 id 与字段名为 null;
    /// 条目级错误尽量补全两者,条目 id 本身不可知时为 null,由原因中的条目序号定位。
    /// </summary>
    public sealed class TableLoadError
    {
        /// <summary>出错的文件名,来自装载入口的传入值。</summary>
        public string FileName { get; }

        /// <summary>出错条目的 id;文件级错误或条目 id 不可知时为 null。</summary>
        public string EntryId { get; }

        /// <summary>出错字段名;文件级错误为 null。</summary>
        public string FieldName { get; }

        /// <summary>错误原因,含定位所需的条目序号或行列信息。</summary>
        public string Reason { get; }

        internal TableLoadError(string fileName, string entryId, string fieldName, string reason)
        {
            FileName = fileName;
            EntryId = entryId;
            FieldName = fieldName;
            Reason = reason;
        }

        /// <summary>按 FileName/EntryId/FieldName/Reason 顺序拼接的定位串。</summary>
        public override string ToString()
        {
            string location = FileName;
            if (EntryId != null)
            {
                location += " 条目 " + EntryId;
            }
            if (FieldName != null)
            {
                location += " 字段 " + FieldName;
            }
            return location + ": " + Reason;
        }
    }
}
