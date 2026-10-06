using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 封口不可变数据表:数据只在 BuildFromRows 构造期一次性进入,此后仅可查询。
    /// 行级修改不存在;整表更新 = 用新行列表重建本类型的实例,再经 DataTableManager 替换注册。
    /// </summary>
    public sealed class ValidatedTable<TKey, TValue> : DataTable<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> data;

        private ValidatedTable(Dictionary<TKey, TValue> data)
        {
            this.data = data;
        }

        /// <summary>表内条目数。</summary>
        public override int Count => data.Count;

        /// <summary>
        /// 从内存行列表构造封口表,全程不接触任何序列化文本。
        /// 行键重复时抛出 <see cref="InvalidOperationException"/>,本次构造整体失败。
        /// </summary>
        /// <param name="rows">全部数据行;构造只读取该序列,完成后与本表无共享可变状态。</param>
        /// <param name="keySelector">从行提取表键的函数,例如 row => row.id。</param>
        /// <exception cref="ArgumentNullException">rows 或 keySelector 为 null。</exception>
        /// <exception cref="InvalidOperationException">两行提取出相同键。</exception>
        public static ValidatedTable<TKey, TValue> BuildFromRows(
            IEnumerable<TValue> rows,
            Func<TValue, TKey> keySelector)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var data = new Dictionary<TKey, TValue>();
            foreach (TValue row in rows)
            {
                TKey key = keySelector(row);
                if (data.ContainsKey(key))
                {
                    throw new InvalidOperationException($"行键重复: {key},同一表内行键必须唯一。");
                }
                data.Add(key, row);
            }
            return new ValidatedTable<TKey, TValue>(data);
        }

        /// <summary>按键取行;键不存在时返回 default,不抛异常。</summary>
        public override TValue Get(TKey key)
        {
            return data.TryGetValue(key, out TValue value) ? value : default;
        }

        /// <summary>按键取行;键不存在时返回 defaultValue。</summary>
        public override TValue GetOrDefault(TKey key, TValue defaultValue)
        {
            return data.GetValueOrDefault(key, defaultValue);
        }

        /// <summary>键是否存在于表中。</summary>
        public override bool ContainsKey(TKey key)
        {
            return data.ContainsKey(key);
        }

        /// <summary>封口表不支持装载。装载管线应先反序列化出行列表,再经 BuildFromRows 构造。</summary>
        /// <exception cref="NotSupportedException">任何调用。</exception>
        public override void Load(string source = null)
        {
            throw new NotSupportedException(
                "ValidatedTable 是封口表,请经 BuildFromRows 用行列表构造新实例。");
        }
    }
}
