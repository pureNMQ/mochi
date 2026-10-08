using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 表声明:装载一张表所需的全部元数据。行键恒为 <see cref="TableRow.id"/>,不再单独声明键维度。
    /// 必填字段列表不含键字段,键字段始终必填。
    /// </summary>
    public sealed class TableDefinition<TRow> where TRow : TableRow
    {
        private readonly List<TableReference> references = new List<TableReference>();
        private readonly List<ITableEditRule<TRow>> editRules =
            new List<ITableEditRule<TRow>>();

        /// <summary>表名,即 DataTableManager 中的注册名。</summary>
        public string TableName { get; }

        /// <summary>除键字段外的必填字段名。</summary>
        public IReadOnlyList<string> RequiredFields { get; }

        /// <summary>跨表引用声明;仅编辑期校验消费,装载管线不校验引用。</summary>
        public IReadOnlyList<TableReference> References => references;

        /// <summary>领域自定义编辑期规则;仅编辑期执行,装载时不执行。</summary>
        public IReadOnlyList<ITableEditRule<TRow>> EditRules => editRules;

        /// <summary>
        /// 声明一张表。
        /// </summary>
        /// <param name="tableName">表名,用作注册名。</param>
        /// <param name="requiredFields">除键字段外的必填字段名。</param>
        /// <exception cref="ArgumentException">tableName 为 null 或空,或 requiredFields 含空项。</exception>
        public TableDefinition(
            string tableName,
            params string[] requiredFields)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("表名不能为空。", nameof(tableName));
            }
            if (requiredFields == null)
            {
                requiredFields = Array.Empty<string>();
            }
            foreach (string field in requiredFields)
            {
                if (string.IsNullOrEmpty(field))
                {
                    throw new ArgumentException("必填字段名不能含有 null 或空串。", nameof(requiredFields));
                }
            }

            TableName = tableName;
            RequiredFields = requiredFields;
        }

        /// <summary>
        /// 声明一个跨表引用并返回自身,供链式构造。
        /// </summary>
        /// <param name="fieldName">存放引用键的字段名。</param>
        /// <param name="targetTableName">被引用表的表名。</param>
        /// <exception cref="ArgumentException">fieldName 或 targetTableName 为 null 或空。</exception>
        public TableDefinition<TRow> AddReference(string fieldName, string targetTableName)
        {
            references.Add(new TableReference(fieldName, targetTableName));
            return this;
        }

        /// <summary>
        /// 挂入一条领域编辑期规则并返回自身,供链式构造。
        /// </summary>
        /// <param name="rule">领域规则。</param>
        /// <exception cref="ArgumentNullException">rule 为 null。</exception>
        public TableDefinition<TRow> AddRule(ITableEditRule<TRow> rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }
            editRules.Add(rule);
            return this;
        }
    }
}
