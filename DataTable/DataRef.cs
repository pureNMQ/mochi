using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Mochi.DataTable
{
    /// <summary>
    /// 惰性行引用:只持有目标行的 string id,首次解析时经 DataTableManager 按行类型定位所在表。
    /// 悬空引用不在装载期兜底,目标行不存在时 <see cref="Resolve"/> 返回 null,由消费端以空引用显现。
    /// </summary>
    /// <typeparam name="TRow">目标行类型;其所在表以行类型在 DataTableManager 的注册为准。</typeparam>
    public struct DataRef<TRow> : IDataRef<TRow> where TRow : TableRow
    {
        private TRow cached;

        /// <summary>目标行的 id;null 或空串表示无引用。</summary>
        public string Id { get; }

        /// <param name="id">目标行的 id。</param>
        public DataRef(string id)
        {
            Id = id;
            cached = null;
        }

        /// <summary>
        /// 解析目标行:首次调用查表并缓存在本副本,结构体按值复制,各副本各自缓存。
        /// 目标表未注册时抛 <see cref="InvalidOperationException"/>;
        /// id 为 null、空串或表内不存在时返回 null,不抛异常。
        /// </summary>
        public TRow Resolve()
        {
            if (cached != null)
            {
                return cached;
            }
            if (string.IsNullOrEmpty(Id))
            {
                return null;
            }
            cached = DataTableManager.Instance.GetDataTable<TRow>().Get(Id);
            return cached;
        }
    }

    /// <summary>
    /// <see cref="DataRef{TRow}"/> 的 JSON 形状契约:读写均为纯字符串 id,不产生对象包裹。
    /// </summary>
    public sealed class DataRefJsonConverter : JsonConverter
    {
        /// <summary>仅接受 DataRef 泛型的封闭类型。</summary>
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType
                && objectType.GetGenericTypeDefinition() == typeof(DataRef<>);
        }

        /// <summary>
        /// 读入纯字符串 id 构造 DataRef;JSON null 产生无引用的默认实例,
        /// 非字符串形状属于数据错误,抛 <see cref="JsonSerializationException"/>。
        /// </summary>
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return Activator.CreateInstance(objectType);
            }
            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException(
                    $"DataRef 字段必须是字符串 id,实际为 {reader.TokenType}。");
            }
            return Activator.CreateInstance(objectType, new object[] { (string)reader.Value });
        }

        /// <summary>写出 Id 的纯字符串值。</summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            PropertyInfo idProperty = value.GetType().GetProperty("Id");
            writer.WriteValue((string)idProperty.GetValue(value));
        }
    }
}
