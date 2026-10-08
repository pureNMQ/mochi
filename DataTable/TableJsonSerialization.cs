using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mochi.DataTable
{
    /// <summary>
    /// 数据表行序列化的演化容错设置:字段缺失或类型不匹配时取默认值、装载不因单字段失败而中断;
    /// 带 <see cref="FormerNameAttribute"/> 的成员按旧名回退读取、写当前名;
    /// <see cref="DataRef{TRow}"/> 字段按纯字符串 id 读写。
    /// 顶层形状(非行对象数组、行非对象)不归本设置兜底,由装载管线的结构校验拒绝。
    /// </summary>
    public static class TableJsonSerialization
    {
        /// <summary>
        /// 构造启用演化容错的序列化设置;每次调用返回独立实例,调用方可继续追加转换器。
        /// </summary>
        public static JsonSerializerSettings CreateEvolutionSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new FormerNameContractResolver(),
            };
            settings.Converters.Add(new DataRefJsonConverter());
            settings.Error += HandleMemberLevelError;
            return settings;
        }

        private static void HandleMemberLevelError(object sender, ErrorEventArgs args)
        {
            //成员或嵌套路径上的错误吞掉,让该成员保持默认值;
            //顶层形状错误的 Path 与 Member 均为空,保持抛出交由结构校验裁决
            if (args.ErrorContext.Member != null || !string.IsNullOrEmpty(args.ErrorContext.Path))
            {
                args.ErrorContext.Handled = true;
            }
        }
    }

    /// <summary>
    /// 为带 <see cref="FormerNameAttribute"/> 的成员追加只读的旧名映射属性:
    /// 反序列化按旧名也能命中同一成员,序列化只写当前名。
    /// </summary>
    internal sealed class FormerNameContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(
            Type type,
            MemberSerialization memberSerialization)
        {
            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            var expanded = new List<JsonProperty>(properties.Count);
            foreach (JsonProperty property in properties)
            {
                expanded.Add(property);

                MemberInfo member = FindMember(type, property.UnderlyingName);
                if (member == null)
                {
                    continue;
                }
                var formerNames =
                    (FormerNameAttribute)member.GetCustomAttribute(typeof(FormerNameAttribute), false);
                if (formerNames == null)
                {
                    continue;
                }
                foreach (string formerName in formerNames.Names)
                {
                    expanded.Add(CreateFallbackProperty(property, formerName));
                }
            }
            return expanded;
        }

        private static MemberInfo FindMember(Type type, string underlyingName)
        {
            MemberInfo[] members = type.GetMember(
                underlyingName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return members.Length > 0 ? members[0] : null;
        }

        private static JsonProperty CreateFallbackProperty(JsonProperty current, string formerName)
        {
            return new JsonProperty
            {
                PropertyName = formerName,
                PropertyType = current.PropertyType,
                ValueProvider = current.ValueProvider,
                Converter = current.Converter,
                ItemConverter = current.ItemConverter,
                Readable = true,
                Writable = false,
            };
        }
    }
}
