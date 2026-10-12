using System;
using System.IO;
using Mochi.DataTable;
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
