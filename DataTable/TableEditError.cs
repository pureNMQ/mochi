using System;

namespace Mochi.DataTable
{
    /// <summary>
    /// 一条编辑期校验错误:定位到条目与字段,消息由校验器或领域规则给出。
    /// 与 <see cref="TableLoadError"/> 的分工:装载错误定位到文件,编辑错误定位到条目,无文件语义。
    /// </summary>
    public sealed class TableEditError
    {
        /// <summary>出错条目的键;条目键不可知时为 null。由校验器统一回填,规则构造时无须填写。</summary>
        public string EntryId { get; internal set; }

        /// <summary>出错字段名;条目级整体错误为 null。</summary>
        public string FieldName { get; }

        /// <summary>错误消息;领域规则产生的错误,其文本来自规则自身。</summary>
        public string Message { get; }

        /// <summary>
        /// 构造一条编辑期错误。
        /// </summary>
        /// <param name="fieldName">出错字段名;条目级整体错误传 null。</param>
        /// <param name="message">错误消息。</param>
        /// <exception cref="ArgumentException">message 为 null 或空。</exception>
        public TableEditError(string fieldName, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("错误消息不能为空。", nameof(message));
            }

            FieldName = fieldName;
            Message = message;
        }

        /// <summary>按 条目/字段/消息 拼接的定位串。</summary>
        public override string ToString()
        {
            string location = "";
            if (EntryId != null)
            {
                location = "条目 " + EntryId + " ";
            }
            if (FieldName != null)
            {
                location += "字段 " + FieldName + " ";
            }
            return location + Message;
        }
    }
}
