namespace Mochi.DataTable
{
    /// <summary>
    /// 数据表的查询契约:行类型必须继承 <see cref="TableRow"/>,表键恒为行 id 字符串。
    /// </summary>
    /// <typeparam name="TRow">行数据类型,必须继承 <see cref="TableRow"/>。</typeparam>
    public abstract class DataTable<TRow> where TRow : TableRow
    {
        /// <summary>表内条目数。</summary>
        public abstract int Count { get; }

        /// <summary>按行 id 取行;id 不存在时返回 null,不抛异常。</summary>
        public abstract TRow Get(string id);

        /// <summary>按行 id 取行;id 不存在时返回 defaultRow。</summary>
        public abstract TRow GetOrDefault(string id, TRow defaultRow);

        /// <summary>行 id 是否存在于表中。</summary>
        public abstract bool ContainsKey(string id);

        /// <summary>装载整表。实现自定来源语义;封口实现不支持装载。</summary>
        public abstract void Load(string source = null);
    }
}
