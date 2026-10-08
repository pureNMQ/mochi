using System;

namespace Mochi.DataTable
{
    /// <summary>
    /// 表声明:装载一张表所需的全部元数据。行键恒为 <see cref="TableRow.id"/>,
    /// 结构与类型校验由数据表浏览器按行类型反射执行,本类型不承载编辑期声明维度。
    /// </summary>
    public sealed class TableDefinition<TRow> where TRow : TableRow
    {
        /// <summary>表名,即 DataTableManager 中的注册名。</summary>
        public string TableName { get; }

        /// <summary>
        /// 声明一张表。
        /// </summary>
        /// <param name="tableName">表名,用作注册名。</param>
        /// <exception cref="ArgumentException">tableName 为 null 或空。</exception>
        public TableDefinition(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("表名不能为空。", nameof(tableName));
            }

            TableName = tableName;
        }
    }
}
