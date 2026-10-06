namespace Mochi.DataTable
{
    /// <summary>
    /// 编辑期跨表键解析:按表名查询某键是否存在于该表当前数据。
    /// 由编辑工具基于其全部表的数据快照实现;键以字符串形式比较。
    /// </summary>
    public interface IEditTimeKeyResolver
    {
        /// <summary>查询表 tableName 的当前数据中是否存在键 key。</summary>
        bool ContainsKey(string tableName, string key);
    }
}
