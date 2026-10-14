using System;
using System.Collections.Generic;
using Mochi.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mochi.Save
{
    /// <summary>
    /// 存档节的注册与调度中心:显式注册的 <see cref="ISaveSection{TData}"/> 就是档文件里允许出现的全部节,
    /// <see cref="CaptureAll"/> 与 <see cref="RestoreAll"/> 成对驱动它们,本类型不认识任何具体业务节。
    /// 注册表在运行时持节的数据类型,档文件只存形状,因此节的类型名不进入存档格式。
    /// </summary>
    public sealed class SaveCabinet
    {
        /// <summary>当前代码支持的档格式世代号,写档时写入文档头。</summary>
        public const int CurrentVersion = 1;

        private readonly List<Registration> registrations = new List<Registration>();
        private readonly ILogger log;

        /// <param name="log">日志记录器;为 null 时取 <see cref="LogManager"/> 的本类型分类默认实例。</param>
        public SaveCabinet(ILogger log = null)
        {
            this.log = log ?? LogManager.GetLogger(nameof(SaveCabinet));
        }

        /// <summary>
        /// 文档头 BuildVersion 的来源,由宿主在构造后注入(例如 MochiUnity 取 Application.version)。
        /// Mochi 不引用引擎,未注入时头里写空串,AC1 要求的构建版本值依赖宿主装配时提供。
        /// </summary>
        public Func<string> BuildVersionProvider { get; set; }

        /// <summary>
        /// 注册一个节。注册清单是唯一权威:档中出现未注册节时读档会告警并跳过。
        /// </summary>
        /// <typeparam name="TData">节的数据形状。</typeparam>
        /// <param name="section">节实现,其 <see cref="ISaveSection{TData}.SectionId"/> 不得为 null 或空白。</param>
        /// <exception cref="ArgumentNullException">section 为 null。</exception>
        /// <exception cref="InvalidOperationException">SectionId 为 null 或空白,或该 id 已被注册;已注册的节不会被覆盖。</exception>
        public void Register<TData>(ISaveSection<TData> section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            string sectionId = section.SectionId;
            if (string.IsNullOrWhiteSpace(sectionId))
            {
                throw new InvalidOperationException("存档节的 SectionId 不能为 null 或空白。");
            }
            if (Find(sectionId) != null)
            {
                throw new InvalidOperationException($"存档节 id 已被注册,不能覆盖:{sectionId}。");
            }

            registrations.Add(new Registration
            {
                SectionId = sectionId,
                DataType = typeof(TData),
                Capture = () => (object)section.Capture(),
                Restore = data => section.Restore(data == null ? default(TData) : (TData)data),
            });
        }

        /// <summary>
        /// 生成当前全量快照:文档头写 <see cref="CurrentVersion"/>、UTC 写档时刻与
        /// <see cref="BuildVersionProvider"/> 的值,各注册节按注册顺序 Capture 并序列化进
        /// <see cref="SaveDocument.Sections"/>。
        /// </summary>
        /// <returns>可直接交给 <see cref="SaveFileStore.WriteAtomic"/> 的档。</returns>
        /// <exception cref="InvalidOperationException">某个注册节的 Capture() 返回 null;空状态必须以空 DTO 表达。</exception>
        public SaveDocument CaptureAll()
        {
            JsonSerializer serializer = JsonSerializer.Create(SaveJson.CreateSettings());
            var document = new SaveDocument
            {
                Version = CurrentVersion,
                SavedAt = DateTime.UtcNow.ToString("o"),
                BuildVersion = BuildVersionProvider?.Invoke() ?? string.Empty,
            };

            foreach (Registration registration in registrations)
            {
                object data = registration.Capture();
                if (data == null)
                {
                    throw new InvalidOperationException(
                        $"存档节 {registration.SectionId} 的 Capture() 返回 null,空状态必须返回空 DTO。");
                }
                document.Sections[registration.SectionId] = JToken.FromObject(data, serializer);
            }

            return document;
        }

        /// <summary>
        /// 用档中数据恢复各注册节。注册节在档中缺失时告警并跳过,该系统保持初始状态;
        /// 档中存在未注册节时同样告警并跳过。两者都不中断读档,以便新系统上线前写出的老档仍可读。
        /// </summary>
        /// <param name="document">读出的档;<see cref="SaveDocument.Sections"/> 允许为空。</param>
        /// <exception cref="ArgumentNullException">document 为 null。</exception>
        /// <exception cref="Exception">注册节自身的 Restore 抛出异常时原样向调用方传播;调用方按恢复失败处理。</exception>
        public void RestoreAll(SaveDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            IDictionary<string, JToken> sections = document.Sections ?? new Dictionary<string, JToken>();
            JsonSerializer serializer = JsonSerializer.Create(SaveJson.CreateSettings());

            foreach (Registration registration in registrations)
            {
                if (!sections.TryGetValue(registration.SectionId, out JToken token))
                {
                    log.WarnFormat(
                        "存档缺少已注册的节 {0},跳过恢复,该系统保持初始状态。",
                        registration.SectionId);
                    continue;
                }

                object data;
                try
                {
                    data = token == null ? null : token.ToObject(registration.DataType, serializer);
                }
                catch (JsonException exception)
                {
                    log.WarnFormat(
                        "存档节 {0} 的数据形状无法转换,跳过恢复:{1}",
                        registration.SectionId,
                        exception.Message);
                    continue;
                }

                registration.Restore(data);
            }

            foreach (KeyValuePair<string, JToken> section in sections)
            {
                if (Find(section.Key) == null)
                {
                    log.WarnFormat("存档含未注册的节 {0},跳过。", section.Key);
                }
            }
        }

        private Registration Find(string sectionId)
        {
            foreach (Registration registration in registrations)
            {
                if (string.Equals(registration.SectionId, sectionId, StringComparison.Ordinal))
                {
                    return registration;
                }
            }
            return null;
        }

        private sealed class Registration
        {
            public string SectionId;
            public Type DataType;
            public Func<object> Capture;
            public Action<object> Restore;
        }
    }
}
