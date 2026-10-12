using Mochi.DataTable;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mochi.Save
{
    /// <summary>
    /// 存档 JSON 的设置来源:节 DTO 与档文件都经同一套设置读写,形状约定才不会在两边漂移。
    /// 演化容错与数据表行序列化同族(旧字段名回退读取、成员级错误取默认值、DataRef 写纯字符串 id),
    /// 因此老档在新代码下仍可读。
    /// </summary>
    public static class SaveJson
    {
        /// <summary>
        /// 构造存档序列化设置;每次调用返回独立实例,调用方可继续追加转换器。
        /// </summary>
        public static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // 允许类型名进档会让类名成为档格式的一部分,重构或改名即废档
                TypeNameHandling = TypeNameHandling.None,
                Formatting = Formatting.Indented,
                ContractResolver = new FormerNameContractResolver(),
            };
            settings.Converters.Add(new DataRefJsonConverter());
            settings.Error += HandleMemberLevelError;
            return settings;
        }

        private static void HandleMemberLevelError(object sender, ErrorEventArgs args)
        {
            // 成员级错误让该成员保持默认值;顶层形状错误(Path 与 Member 均为空)保持抛出,
            // 由取值方决定是弃档还是抛给调用方
            if (args.ErrorContext.Member != null || !string.IsNullOrEmpty(args.ErrorContext.Path))
            {
                args.ErrorContext.Handled = true;
            }
        }
    }
}
