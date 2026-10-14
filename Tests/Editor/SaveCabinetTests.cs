using System;
using System.Collections.Generic;
using System.IO;
using Mochi.DataTable;
using Mochi.Logging;
using Mochi.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Mochi.Tests
{
    public class SaveCabinetTests
    {
        private sealed class SpeciesRow : TableRow
        {
        }

        private sealed class PartySaveData
        {
            public string leaderId;
            public DataRef<SpeciesRow> speciesId;
        }

        private sealed class PartySection : ISaveSection<PartySaveData>
        {
            public string SectionId => "party";

            public PartySaveData Data = new PartySaveData();

            public PartySaveData Capture()
            {
                return Data;
            }

            public void Restore(PartySaveData data)
            {
                Data = data;
            }
        }

        // 验收条件 AC11:当同一节 id 被重复注册，则注册时抛出异常。
        [Test]
        public void SaveCabinet_Register_DuplicateSectionId_Throws()
        {
            SaveCabinet cabinet = new SaveCabinet();
            cabinet.Register(new PartySection());

            Assert.That(
                () => cabinet.Register(new PartySection()),
                Throws.TypeOf<InvalidOperationException>());
        }

        // 验收条件 AC9:当档含未注册节、或已注册节在档中缺失、或档内表 id（如 speciesId）在表中不存在，则读档照常完成且逐项记录日志（缺失节系统保持初始状态、多余节跳过、悬空 id 条目跳过）。
        [Test]
        public void SaveCabinet_RestoreAll_ToleratesMissingUnknownAndMalformedSections()
        {
            var section = new PartySection
            {
                Data = new PartySaveData { leaderId = "initial" },
            };
            var cabinet = new SaveCabinet();
            cabinet.Register(section);
            var missingSection = new MissingSection { Value = 42 };
            cabinet.Register(missingSection);

            var log = new CapturingLogWriter();
            LogManager.AddWriter(log);
            try
            {
                //档里省略 missing.section,用 party 制造形状错误,并额外放入未注册节
                var document = new SaveDocument();
                document.Sections["party"] = JArray.Parse("[1,2,3]");
                document.Sections["unknown.section"] = JObject.Parse("{\"value\":1}");

                Assert.That(() => cabinet.RestoreAll(document), Throws.Nothing);
                Assert.That(section.Data.leaderId, Is.EqualTo("initial"), "形状不兼容时该系统保持初始状态");
                Assert.That(missingSection.Value, Is.EqualTo(42), "缺失节保持初始状态");
            }
            finally
            {
                LogManager.RemoveWriter(log);
            }

            Assert.That(
                log.MessagesWith("party").Count,
                Is.EqualTo(1),
                "形状不兼容的节必须记一条日志");
            Assert.That(
                log.MessagesWith("unknown.section").Count,
                Is.EqualTo(1),
                "未注册的节必须记一条日志");
            Assert.That(log.MessagesWith("missing.section").Count, Is.EqualTo(1), "缺失节必须记一条日志");
        }

        // 支撑性测试,非验收条件:存档层从不解析表引用,因此悬空 id 不会中断恢复。
        // 解析发生在消费端,且 DataRef 的契约是"未注册表类型抛异常、表内无此 id 返回 null",
        // 这两条都由 DataTable 侧保证,不在本测试范围内。
        [Test]
        public void SaveCabinet_RestoreAll_DanglingTableReferenceDoesNotAbortRestore()
        {
            var section = new PartySection
            {
                Data = new PartySaveData { leaderId = "initial" },
            };
            var cabinet = new SaveCabinet();
            cabinet.Register(section);

            var document = new SaveDocument();
            document.Sections["party"] = JObject.Parse(
                "{\"leaderId\":\"player\",\"speciesId\":\"species_does_not_exist\"}");

            Assert.That(() => cabinet.RestoreAll(document), Throws.Nothing);
            Assert.That(section.Data.leaderId, Is.EqualTo("player"));
            Assert.That(
                section.Data.speciesId.Id,
                Is.EqualTo("species_does_not_exist"),
                "悬空 id 原样留在 DTO 里,恢复不因它失败");
        }

        private sealed class MissingSection : ISaveSection<int>
        {
            public int Value;
            public string SectionId => "missing.section";
            public int Capture() => Value;
            public void Restore(int data) => Value = data;
        }

        private sealed class CapturingLogWriter : ILogWriter
        {
            private readonly List<string> messages = new List<string>();

            public LogLevel MinLevel { get; set; } = LogLevel.Debug;

            public void WriteLog(LogEntry entry)
            {
                messages.Add(entry.Message);
            }

            public void Flush()
            {
            }

            public void Dispose()
            {
            }

            public List<string> MessagesWith(string text)
            {
                var matched = new List<string>();
                foreach (string message in messages)
                {
                    if (message != null && message.Contains(text))
                    {
                        matched.Add(message);
                    }
                }

                return matched;
            }
        }

        // 支撑性测试，非验收条件:capture 写档再读档恢复的往返保真，DataRef 字段落为纯字符串 id。
        [Test]
        public void SaveCabinet_CaptureWriteReadRestore_RoundTripsSectionData()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "MochiSaveTests_" + Guid.NewGuid().ToString("N"));

            try
            {
                var source = new PartySection
                {
                    Data = new PartySaveData
                    {
                        leaderId = "player",
                        speciesId = new DataRef<SpeciesRow>("species_001"),
                    },
                };
                var cabinet = new SaveCabinet { BuildVersionProvider = () => "1.2.3" };
                cabinet.Register(source);

                SaveDocument captured = cabinet.CaptureAll();

                Assert.That(captured.Version, Is.EqualTo(SaveCabinet.CurrentVersion));
                Assert.That(captured.BuildVersion, Is.EqualTo("1.2.3"));
                Assert.That(
                    captured.Sections["party"]["speciesId"].Type,
                    Is.EqualTo(JTokenType.String));
                Assert.That(
                    (string)captured.Sections["party"]["speciesId"],
                    Is.EqualTo("species_001"));

                var store = new SaveFileStore(directory);
                store.WriteAtomic(captured);

                SaveDocument read = store.Read();
                Assert.That(read, Is.Not.Null);

                var restored = new PartySection();
                var restoringCabinet = new SaveCabinet();
                restoringCabinet.Register(restored);
                restoringCabinet.RestoreAll(read);

                Assert.That(restored.Data.leaderId, Is.EqualTo("player"));
                Assert.That(restored.Data.speciesId.Id, Is.EqualTo("species_001"));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}
