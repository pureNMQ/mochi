using Mochi.Logging;

namespace Mochi.Save
{
    /// <summary>版本保护判定档是否可用,以及不可用时该向玩家说什么。</summary>
    public enum SaveGuardReason
    {
        /// <summary>档可用。</summary>
        Usable,

        /// <summary>没有档可读。</summary>
        NoSave,

        /// <summary>档的世代号超出当前代码支持的范围。</summary>
        VersionTooNew,

        /// <summary>主档与备份都读不出来。</summary>
        Corrupted,
    }

    /// <summary>一次版本保护判定的结果:可用的档、不可用原因与被判定时的诊断信息。</summary>
    public readonly struct SaveGuardResult
    {
        internal SaveGuardResult(
            bool canUse,
            SaveGuardReason reason,
            SaveDocument document,
            string detail,
            bool recoveredFromBackup = false)
        {
            CanUse = canUse;
            Reason = reason;
            Document = document;
            Detail = detail;
            RecoveredFromBackup = recoveredFromBackup;
        }

        /// <summary>档是否可用;为 true 时 <see cref="Document"/> 非 null。</summary>
        public bool CanUse { get; }

        /// <summary>不可用的原因;可用时为 <see cref="SaveGuardReason.Usable"/>。</summary>
        public SaveGuardReason Reason { get; }

        /// <summary>可用的档;<see cref="CanUse"/> 为 false 时为 null。</summary>
        public SaveDocument Document { get; }

        /// <summary>诊断细节(如多余的世代号),只进日志;玩家可见文案由宿主按构建类型决定。</summary>
        public string Detail { get; }

        /// <summary>
        /// 这份可用的档是否来自备份回退(<see cref="SaveGuardReason.Corrupted"/> 对应的场景)。
        /// 宿主据此给出"已从备份恢复"提示——这是 AC7 与 AC8 的分水岭:
        /// 两者同样源于主档不可读,差别在于备份能不能顶上。
        /// </summary>
        public bool RecoveredFromBackup { get; }
    }

    /// <summary>
    /// 存档的版本保护:在主档与备份里选出可用的一份,并拦下世代号超出当前代码支持范围的档。
    /// 判定是只读的;把过新的主档与备份成对隔离是宿主的显式动作(<see cref="SaveFileStore.QuarantinePair"/>),
    /// 因为"看一眼"不该改变磁盘状态。
    /// </summary>
    public sealed class SaveVersionGuard
    {
        /// <summary>可续玩的档世代号下限;低于它的档头字段必然缺失或全为默认值。</summary>
        public const int MinimumSupportedVersion = 1;

        private readonly SaveFileStore store;
        private readonly int currentVersion;
        private readonly ILogger log;

        /// <param name="store">读取档的文件存储。</param>
        /// <param name="currentVersion">当前代码支持的档世代号。</param>
        /// <param name="log">日志记录器;为 null 时取本类型分类的默认实例。</param>
        public SaveVersionGuard(SaveFileStore store, int currentVersion, ILogger log = null)
        {
            this.store = store;
            this.currentVersion = currentVersion;
            this.log = log ?? LogManager.GetLogger(nameof(SaveVersionGuard));
        }

        /// <summary>
        /// 判定可用的档:主档优先,主档不可用且存在备份时回退备份;
        /// 两份都不可用、或世代号超出当前支持范围时判为不可用。
        /// <para>
        /// "主档不可用"包含两种形态:读不出来(null),以及读得出来但不可信(世代号超出支持范围或缺失)。
        /// 后者同样要走备份回退——一个可解析但字段全默认的残档,若因为它"读出来了"就短路掉回退,
        /// 反过来会让完好的备份永远接不上(AC7 失效)。
        /// </para>
        /// </summary>
        public SaveGuardResult Inspect()
        {
            SaveDocument main = store.Read();
            SaveGuardResult mainVerdict = main == null
                ? default
                : Judge(main, "主档");

            // 主档直接可用
            if (main != null && mainVerdict.CanUse)
            {
                return mainVerdict;
            }

            // 主档世代号超前:这是整份档的共同状态(主档与备份出自同一写档路径),
            // 处置是成对隔离而不是回退备份——回退会让宿主把一场版本不兼容报成"已从备份恢复"。
            if (main != null && mainVerdict.Reason == SaveGuardReason.VersionTooNew)
            {
                return mainVerdict;
            }

            if (!store.BackupExists)
            {
                if (main != null)
                {
                    // 主档在但不可信,且没有备份可回退
                    return mainVerdict;
                }

                // 主档读不出来时,"没有备份"有两种处境,提示与否由磁盘上到底有没有档文件决定:
                // 一个档文件都没有 = 无档(AC2 的静默置灰);存在读不出来的档 = 坏档(AC8 的提示)。
                return store.Exists
                    ? new SaveGuardResult(false, SaveGuardReason.Corrupted, null, "主档不可读且没有备份")
                    : new SaveGuardResult(false, SaveGuardReason.NoSave, null, "主档与备份都不存在");
            }

            SaveDocument backup = store.ReadBackup();

            // 主档不可用(读不出来,或读得出来但不可信)而备份可用:回退备份并说明来源
            if (backup != null)
            {
                SaveGuardResult backupVerdict = Judge(backup, "备份");
                if (backupVerdict.CanUse)
                {
                    log.Warn("主档不可用,改用备份档恢复。");
                    return new SaveGuardResult(
                        true,
                        SaveGuardReason.Corrupted,
                        backup,
                        "主档不可用,已回退备份",
                        recoveredFromBackup: true);
                }

                // 备份自身超前(比主档更不可容忍):报备份的问题
                if (backupVerdict.Reason == SaveGuardReason.VersionTooNew)
                {
                    return backupVerdict;
                }
            }

            // 备份也读不出来或不可信:坏文件保持原样,不做任何删改
            return new SaveGuardResult(
                false,
                SaveGuardReason.Corrupted,
                null,
                main == null ? "主档与备份都读不出来" : "主档不可信且备份不可用");
        }

        private SaveGuardResult Judge(SaveDocument document, string source)
        {
            if (document.Version > currentVersion)
            {
                return new SaveGuardResult(
                    false,
                    SaveGuardReason.VersionTooNew,
                    null,
                    $"{source}世代号 {document.Version} 高于当前支持的 {currentVersion}");
            }

            // 世代号是每份档都必写的头字段。缺失或为 0 只可能来自损坏、截断或字段全默认的空对象
            // (例如被截断的 `{"corrupt":` 会被解析成一个合法的空档),这种档一旦放行,
            // 玩家会带着全空进度"继续",进度被静默丢弃——比读不出来更糟。
            if (document.Version < MinimumSupportedVersion)
            {
                return new SaveGuardResult(
                    false,
                    SaveGuardReason.Corrupted,
                    null,
                    $"{source}世代号为 {document.Version},不是可续玩的档");
            }

            return new SaveGuardResult(true, SaveGuardReason.Usable, document, null);
        }
    }
}
