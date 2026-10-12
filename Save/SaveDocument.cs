using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mochi.Save
{
    /// <summary>
    /// 一档的全量快照:文档头描述这档由哪个构建在何时写出,<see cref="Sections"/> 按节 id 存放各节的 JSON 形状。
    /// 头只承载排障线索,可变状态一律放节,以免头随业务增长而需要独立的字段提供者机制。
    /// </summary>
    public sealed class SaveDocument
    {
        /// <summary>档格式世代号。只有不可容忍的结构变更才提升它,节级不符由演化容错消化。</summary>
        public int Version { get; set; }

        /// <summary>写档时刻的 UTC 时间,ISO 8601("o")格式。</summary>
        public string SavedAt { get; set; }

        /// <summary>写档的构建版本。Mochi 不引用引擎,该值由宿主经 <see cref="SaveCabinet.BuildVersionProvider"/> 注入。</summary>
        public string BuildVersion { get; set; }

        /// <summary>
        /// 节 id 到节 JSON 的映射。这里存的是形状而不是 .NET 类型,
        /// 节的类型只在运行时注册表里,因此重命名或移动 DTO 类型不会废档。
        /// </summary>
        public IDictionary<string, JToken> Sections { get; } = new Dictionary<string, JToken>();
    }
}
