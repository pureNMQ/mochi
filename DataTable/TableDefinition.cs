using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 表声明:装载一张表所需的全部元数据。
    /// 键字段名供错误定位与键存在性检查使用;必填字段列表不包含键字段,键字段始终必填。
    /// </summary>
    public sealed class TableDefinition<TKey, TValue>
    {
        private readonly List<TableReference> references = new List<TableReference>();
        private readonly List<ITableEditRule<TKey, TValue>> editRules =
            new List<ITableEditRule<TKey, TValue>>();

        /// <summary>表名,即 DataTableManager 中的注册名。</summary>
        public string TableName { get; }

        /// <summary>键字段名,即 KeySelector 读取的字段在 JSON 中的名字。</summary>
        public string KeyFieldName { get; }

        /// <summary>从行提取表键的函数,例如 row => row.id。</summary>
        public Func<TValue, TKey> KeySelector { get; }

        /// <summary>除键字段外的必填字段名。</summary>
        public IReadOnlyList<string> RequiredFields { get; }

        /// <summary>跨表引用声明;仅编辑期校验消费,装载管线不校验引用。</summary>
        public IReadOnlyList<TableReference> References => references;

        /// <summary>领域自定义编辑期规则;仅编辑期执行,装载时不执行。</summary>
        public IReadOnlyList<ITableEditRule<TKey, TValue>> EditRules => editRules;

        /// <summary>
        /// 声明一张表。
        /// </summary>
        /// <param name="tableName">表名,用作注册名。</param>
        /// <param name="keyFieldName">键字段名。</param>
        /// <param name="keySelector">从行提取表键的函数。</param>
        /// <param name="requiredFields">除键字段外的必填字段名。</param>
        /// <exception cref="ArgumentException">tableName 或 keyFieldName 为 null 或空,或 requiredFields 含空项。</exception>
        /// <exception cref="ArgumentNullException">keySelector 为 null。</exception>
        public TableDefinition(
            string tableName,
            string keyFieldName,
            Func<TValue, TKey> keySelector,
            params string[] requiredFields)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("表名不能为空。", nameof(tableName));
            }
            if (string.IsNullOrEmpty(keyFieldName))
            {
                throw new ArgumentException("键字段名不能为空。", nameof(keyFieldName));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
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
            KeyFieldName = keyFieldName;
            KeySelector = keySelector;
            RequiredFields = requiredFields;
        }

        /// <summary>
        /// 声明一个跨表引用并返回自身,供链式构造。
        /// </summary>
        /// <param name="fieldName">存放引用键的字段名。</param>
        /// <param name="targetTableName">被引用表的表名。</param>
        /// <exception cref="ArgumentException">fieldName 或 targetTableName 为 null 或空。</exception>
        public TableDefinition<TKey, TValue> AddReference(string fieldName, string targetTableName)
        {
            references.Add(new TableReference(fieldName, targetTableName));
            return this;
        }

        /// <summary>
        /// 挂入一条领域编辑期规则并返回自身,供链式构造。
        /// </summary>
        /// <param name="rule">领域规则。</param>
        /// <exception cref="ArgumentNullException">rule 为 null。</exception>
        public TableDefinition<TKey, TValue> AddRule(ITableEditRule<TKey, TValue> rule)
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
