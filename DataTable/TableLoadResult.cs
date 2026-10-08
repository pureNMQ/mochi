using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 单表装载结果。
    /// 成功时 Table 非空且已注册进 DataTableManager,Errors 为空列表;
    /// 失败时 Table 为 null、表未注册,Errors 含该文件内收集到的全部定位记录。
    /// </summary>
    public sealed class TableLoadResult<TRow> where TRow : TableRow
    {
        /// <summary>装载是否成功;成功即代表已完成注册。</summary>
        public bool Succeeded { get; }

        /// <summary>全部装载错误;成功时为空列表。</summary>
        public IReadOnlyList<TableLoadError> Errors { get; }

        /// <summary>装载出的封口表;失败时为 null。</summary>
        public ValidatedTable<TRow> Table { get; }

        internal TableLoadResult(
            bool succeeded,
            IReadOnlyList<TableLoadError> errors,
            ValidatedTable<TRow> table)
        {
            Succeeded = succeeded;
            Errors = errors;
            Table = table;
        }
    }
}
