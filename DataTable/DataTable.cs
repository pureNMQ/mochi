
namespace Mochi.DataTable
{
    public abstract class DataTable<TKey, TValue>
    {
        public abstract int Count { get; }
        public abstract TValue Get(TKey key);
        public abstract TValue GetOrDefault(TKey key, TValue defaultValue);
        public abstract bool ContainsKey(TKey key);
        public abstract void Load(string source = null);
    }
}