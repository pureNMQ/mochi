using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 封口不可变数据表:数据只在 BuildFromRows 构造期一次性进入,此后仅可查询。
    /// 行级修改不存在;整表更新 = 用新行列表重建本类型的实例,再经 DataTableManager 替换注册。
    /// </summary>
    public sealed class ValidatedTable<TRow> : DataTable<TRow> where TRow : TableRow
    {
        private readonly Dictionary<string, TRow> data;

        private ValidatedTable(Dictionary<string, TRow> data)
        {
            this.data = data;
        }

        /// <summary>表内条目数。</summary>
        public override int Count => data.Count;

        /// <summary>
        /// 从内存行列表构造封口表,全程不接触任何序列化文本;行键取每行的 <see cref="TableRow.id"/>。
        /// 行 id 为 null、空串或重复时抛出 <see cref="InvalidOperationException"/>,本次构造整体失败。
        /// </summary>
        /// <param name="rows">全部数据行;构造只读取该序列,完成后与本表无共享可变状态。</param>
        /// <exception cref="ArgumentNullException">rows 为 null。</exception>
        /// <exception cref="InvalidOperationException">行 id 为 null 或空串,或两行 id 重复。</exception>
        public static ValidatedTable<TRow> BuildFromRows(IEnumerable<TRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var data = new Dictionary<string, TRow>();
            foreach (TRow row in rows)
            {
                string key = row.id;
                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException(
                        $"第 {data.Count + 1} 个有效行的 id 为 null 或空串,行 id 必须非空。");
                }
                if (data.ContainsKey(key))
                {
                    throw new InvalidOperationException($"行键重复: {key},同一表内行键必须唯一。");
                }
                data.Add(key, row);
            }
            return new ValidatedTable<TRow>(data);
        }

        /// <summary>按键取行;键不存在时返回 default,不抛异常。</summary>
        public override TRow Get(string id)
        {
            return data.TryGetValue(id, out TRow value) ? value : default;
        }

        /// <summary>按键取行;键不存在时返回 defaultRow。</summary>
        public override TRow GetOrDefault(string id, TRow defaultRow)
        {
            return data.GetValueOrDefault(id, defaultRow);
        }

        /// <summary>键是否存在于表中。</summary>
        public override bool ContainsKey(string id)
        {
            return data.ContainsKey(id);
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
