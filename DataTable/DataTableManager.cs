using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 按表名注册与查询数据表的管理器。
    /// Register 是唯一的首注册入口,同名重复注册属于编程错误,抛出异常且首个注册保持有效;
    /// 合法的整表更新(如远程下发后的整表替换、测试重播种)走 Replace,不经 Register。
    /// </summary>
    public class DataTableManager : Singleton<DataTableManager>
    {
        private readonly Dictionary<string, object> dataTables = new Dictionary<string, object>();

        /// <summary>
        /// 首次注册数据表。
        /// 同名表已存在时抛出 <see cref="InvalidOperationException"/>,已注册的表保持原样;
        /// 整表更新请改用 <see cref="ReplaceDataTable{TKey,TValue}"/>。
        /// </summary>
        /// <exception cref="ArgumentException">name 为 null 或空。</exception>
        /// <exception cref="ArgumentNullException">dataTable 为 null。</exception>
        public void RegisterDataTable<TKey, TValue>(string name, DataTable<TKey, TValue> dataTable)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("表名不能为空。", nameof(name));
            }
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }
            if (dataTables.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"数据表 {name} 已注册。重复 Register 属于编程错误,整表更新请使用 ReplaceDataTable。");
            }
            dataTables[name] = dataTable;
        }

        /// <summary>
        /// 以新表整体替换同名注册;该名字尚未注册时等价于首次注册。
        /// 这是远程下发整表替换等合法更新的入口,不触发重复注册检查。
        /// </summary>
        /// <exception cref="ArgumentException">name 为 null 或空。</exception>
        /// <exception cref="ArgumentNullException">dataTable 为 null。</exception>
        public void ReplaceDataTable<TKey, TValue>(string name, DataTable<TKey, TValue> dataTable)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("表名不能为空。", nameof(name));
            }
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }
            dataTables[name] = dataTable;
        }

        /// <summary>
        /// 按表名取已注册的表。
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 名字未注册,或注册表与请求的 DataTable 键值类型不匹配;消息中含表名。
        /// </exception>
        public DataTable<TKey, TValue> GetDataTable<TKey, TValue>(string name)
        {
            if (!dataTables.TryGetValue(name, out object table))
            {
                throw new InvalidOperationException($"未注册名为 {name} 的数据表。");
            }
            if (!(table is DataTable<TKey, TValue> typedTable))
            {
                throw new InvalidOperationException(
                    $"数据表 {name} 的注册类型是 {table.GetType().FullName}," +
                    $"与请求的 DataTable<{typeof(TKey)}, {typeof(TValue)}> 不匹配。");
            }
            return typedTable;
        }
    }
}
