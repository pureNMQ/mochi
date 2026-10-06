using System;

namespace Mochi.DataTable
{
    /// <summary>
    /// 声明式跨表引用:某字段存放另一张表的条目键。
    /// 标量字符串字段为 null 或空串时视为无引用,不做校验;字符串数组字段逐元素校验。
    /// 仅编辑期校验消费;装载管线不校验引用合法性。
    /// </summary>
    public sealed class TableReference
    {
        /// <summary>存放引用键的字段名。</summary>
        public string FieldName { get; }

        /// <summary>被引用表在 DataTableManager 语义下的表名。</summary>
        public string TargetTableName { get; }

        /// <summary>
        /// 声明一个跨表引用。
        /// </summary>
        /// <param name="fieldName">存放引用键的字段名。</param>
        /// <param name="targetTableName">被引用表的表名。</param>
        /// <exception cref="ArgumentException">fieldName 或 targetTableName 为 null 或空。</exception>
        public TableReference(string fieldName, string targetTableName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentException("引用字段名不能为空。", nameof(fieldName));
            }
            if (string.IsNullOrEmpty(targetTableName))
            {
                throw new ArgumentException("引用目标表名不能为空。", nameof(targetTableName));
            }

            FieldName = fieldName;
            TargetTableName = targetTableName;
        }
    }
}
