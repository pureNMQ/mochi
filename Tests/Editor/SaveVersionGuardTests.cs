using System;
using System.IO;
using Mochi.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Mochi.Tests
{
    /// <summary>
    /// 支撑性测试（不锚定验收条件）：版本保护的判定语义与成对隔离的文件行为。
    /// AC5-AC8 的验收锚点在 Veyra 侧，这里只压低层机制出错的风险。
    /// </summary>
    public sealed class SaveVersionGuardTests
    {
        private string directoryPath;
        private SaveFileStore store;

        [SetUp]
        public void SetUp()
        {
            directoryPath = Path.Combine(
                Path.GetTempPath(), "MochiSaveGuard_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directoryPath);
            store = new SaveFileStore(directoryPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }

        [Test]
        public void Inspect_VersionAheadOfSupported_ReportsTooNewWithoutTouchingDisk()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion + 1));
            byte[] before = File.ReadAllBytes(store.MainFilePath);

            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(result.CanUse, Is.False, "超出支持范围的档不可用");
            Assert.That(result.Reason, Is.EqualTo(SaveGuardReason.VersionTooNew));
            Assert.That(result.Document, Is.Null, "不可用时不得交出档");
            Assert.That(
                File.ReadAllBytes(store.MainFilePath),
                Is.EqualTo(before),
                "判定必须只读：看一眼不得改变磁盘状态");
        }

        [Test]
        public void Inspect_MainUnreadableAndBackupUsable_FallsBackToBackup()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            File.WriteAllText(store.MainFilePath, "not json at all");

            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(result.CanUse, Is.True, "主档损坏而备份可读时必须回退备份");
            Assert.That(result.Document, Is.Not.Null);
            Assert.That(result.RecoveredFromBackup, Is.True, "AC7 的提示依据这一位,不能只看 CanUse");
            Assert.That(
                result.Reason,
                Is.EqualTo(SaveGuardReason.Corrupted),
                "原因仍是主档损坏;回退成功不改变原因");
        }

        [Test]
        public void Inspect_TruncatedObjectParsesAsEmptyDocument_StillRejected()
        {
            // 被截断的 {"corrupt": 会被 Newtonsoft 解析成一个字段全默认的合法 SaveDocument。
            // 这种"可读的空档"一旦放行,玩家会带着全空进度继续,进度被静默丢弃。
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();
            File.WriteAllText(store.MainFilePath, "{\"corrupt\":");

            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(
                result.Reason,
                Is.EqualTo(SaveGuardReason.Corrupted),
                "全默认的空档必须判为损坏,而不是可用");
            Assert.That(result.CanUse, Is.True, "备份可用时必须回退备份,而不是把玩家的进度判死");
            Assert.That(result.RecoveredFromBackup, Is.True, "此时备份可读,应回退备份");
            Assert.That(result.Document, Is.Not.Null);
        }

        [Test]
        public void InjectCorruption_MakesMainUnreadableAndEnablesBackupFallback()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));

            store.InjectCorruption();

            Assert.That(store.Read(), Is.Null, "故障注入必须真的注入出读不出来的主档");
            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();
            Assert.That(result.RecoveredFromBackup, Is.True, "主档坏、备份好时应回退备份");
        }

        [Test]
        public void Inspect_BothUnreadable_ReportsCorruptedAndKeepsFiles()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            File.WriteAllText(store.MainFilePath, "{\"corrupt\":");
            File.WriteAllText(store.BackupFilePath, "{\"alsoCorrupt\":");

            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(result.CanUse, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SaveGuardReason.Corrupted));
            Assert.That(File.Exists(store.MainFilePath), Is.True, "坏文件保留原地");
            Assert.That(File.Exists(store.BackupFilePath), Is.True, "坏文件保留原地");
        }

        [Test]
        public void Inspect_NoFilesAtAll_ReportsNoSave()
        {
            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(result.CanUse, Is.False);
            Assert.That(result.Reason, Is.EqualTo(SaveGuardReason.NoSave));
        }

        [Test]
        public void Inspect_CorruptMainWithoutBackup_ReportsCorruptedNotNoSave()
        {
            // "一个档文件都没有"(AC2 静默置灰)与"有档但读不出来"(AC8 提示无法读取)
            // 是两种玩家处境,判据是磁盘上有没有档文件,而不是读不读得出来。
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            File.WriteAllText(store.MainFilePath, "corrupted-by-injection");

            SaveGuardResult result = new SaveVersionGuard(store, SaveCabinet.CurrentVersion).Inspect();

            Assert.That(result.CanUse, Is.False);
            Assert.That(
                result.Reason,
                Is.EqualTo(SaveGuardReason.Corrupted),
                "存在坏档时不能报无档,否则玩家看到置灰却得不到任何解释");
        }

        [Test]
        public void QuarantinePair_MovesBothFilesAndKeepsContent()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            byte[] mainBefore = File.ReadAllBytes(store.MainFilePath);
            byte[] backupBefore = File.ReadAllBytes(store.BackupFilePath);

            string[] moved = store.QuarantinePair();

            Assert.That(moved.Length, Is.EqualTo(2), "主档与备份都要被隔离");
            Assert.That(store.Exists, Is.False, "隔离后主档不在原位置");
            Assert.That(store.BackupExists, Is.False, "隔离后备份不在原位置");
            Assert.That(
                Directory.GetFiles(directoryPath, SaveFileStore.MainFileName + ".corrupt.*").Length,
                Is.EqualTo(1));
            Assert.That(
                Directory.GetFiles(directoryPath, SaveFileStore.BackupFileName + ".corrupt.*").Length,
                Is.EqualTo(1));
            Assert.That(
                File.ReadAllBytes(Path.Combine(directoryPath, moved[0])),
                Is.EqualTo(mainBefore),
                "隔离只改名，内容原样保留");
            Assert.That(
                File.ReadAllBytes(Path.Combine(directoryPath, moved[1])),
                Is.EqualTo(backupBefore),
                "隔离只改名，内容原样保留");
        }

        [Test]
        public void QuarantinePair_BackupOnly_MovesWhatExists()
        {
            store.WriteAtomic(NewDocument(SaveCabinet.CurrentVersion));
            store.Backup();

            string[] moved = store.QuarantinePair();

            Assert.That(moved.Length, Is.EqualTo(1), "只有备份存在时只挪备份");
            Assert.That(store.Exists, Is.False);
            Assert.That(store.BackupExists, Is.False);
        }

        private static SaveDocument NewDocument(int version)
        {
            var document = new SaveDocument
            {
                Version = version,
                SavedAt = DateTime.UtcNow.ToString("o"),
                BuildVersion = "test-build",
            };
            document.Sections["mochi.test.guard"] = JObject.FromObject(new GuardData { value = 5 });
            return document;
        }

        private sealed class GuardData
        {
            public int value;
        }
    }
}
