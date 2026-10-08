using System;
using System.Collections.Generic;

namespace Mochi.DataTable
{
    /// <summary>
    /// 标注字段或属性在旧版数据文件中的名字:反序列化时当前名字缺失则回退按旧名读取,
    /// 序列化始终写当前名字,数据保存一次即完成改名迁移。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class FormerNameAttribute : Attribute
    {
        /// <summary>全部可回退读取的旧字段名。</summary>
        public IReadOnlyList<string> Names { get; }

        /// <param name="names">旧字段名,至少一个。</param>
        /// <exception cref="ArgumentException">names 为 null 或不含任何名字。</exception>
        public FormerNameAttribute(params string[] names)
        {
            if (names == null || names.Length == 0)
            {
                throw new ArgumentException("至少提供一个旧字段名。", nameof(names));
            }
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("旧字段名不能为 null 或空串。", nameof(names));
                }
            }
            Names = names;
        }
    }
}
