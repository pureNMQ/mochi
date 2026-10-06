using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 领域自定义的编辑期校验规则,挂在 <see cref="TableDefinition{TKey,TValue}"/> 上。
    /// 只在编辑期执行,错误消息由规则自身给出;装载管线不执行这些规则,因此规则违规不会被装载拒绝。
    /// </summary>
    /// <typeparam name="TKey">表键类型。</typeparam>
    /// <typeparam name="TValue">行数据类型。</typeparam>
    public interface ITableEditRule<TKey, TValue>
    {
        /// <summary>
        /// 校验一行已反序列化的数据,返回全部违规,无违规时返回空。
        /// <see cref="TableEditError.EntryId"/> 由校验器统一回填,规则无须填写。
        /// </summary>
        /// <param name="row">单行数据。</param>
        /// <param name="keyResolver">跨表键解析器,供规则自行检查引用。</param>
        IEnumerable<TableEditError> Validate(TValue row, IEditTimeKeyResolver keyResolver);
    }
}
