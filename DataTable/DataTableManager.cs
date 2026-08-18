using System;
using System.Collections.Generic;
namespace Mochi.DataTable
{
    public class DataTableManager : Singleton<DataTableManager>
    {
        private readonly Dictionary<Type, object> dataTables = new Dictionary<Type, object>();

        public void RegisterDataTable<TKey, TValue>(DataTable<TKey, TValue> dataTable)
        {
            dataTables[typeof(TValue)] = dataTable;
        }

        public DataTable<TKey, TValue> GetDataTable<TKey, TValue>()
        {
            if (dataTables.TryGetValue(typeof(TValue), out var table))
            {
                return table as DataTable<TKey, TValue>;
            }
            throw new Exception($"DataTable for type {typeof(TValue)} not found.");
        }
    }
}