using System;
using System.IO;
using Mochi.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Mochi.Tests
{
    public class SaveFileStoreTests
    {
        private string directory;

        [SetUp]
        public void CreateTempDirectory()
        {
            directory = Path.Combine(
                Path.GetTempPath(),
                "MochiSaveTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void DeleteTempDirectory()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        // 验收条件 AC12:当写档完成，则主档为完整可解析 JSON（不存在半写状态）。
        [Test]
        public void SaveFileStore_WriteAtomic_MainFileIsCompleteParseableJson()
        {
            var store = new SaveFileStore(directory);

            store.WriteAtomic(CreateDocument("pikachu"));
            AssertMainFileIsComplete(store, "pikachu");

            store.WriteAtomic(CreateDocument("raichu"));
            AssertMainFileIsComplete(store, "raichu");
        }

        private static SaveDocument CreateDocument(string firstMember)
        {
            var document = new SaveDocument
            {
                Version = SaveCabinet.CurrentVersion,
                SavedAt = DateTime.UtcNow.ToString("o"),
                BuildVersion = "test-build",
            };
            document.Sections["party"] = new JObject
            {
                ["members"] = new JArray(firstMember),
            };
            return document;
        }

        private static void AssertMainFileIsComplete(SaveFileStore store, string expectedFirstMember)
        {
            Assert.That(store.Exists, Is.True);

            JObject main = JObject.Parse(File.ReadAllText(store.MainFilePath));

            Assert.That(main["Version"].Value<int>(), Is.EqualTo(SaveCabinet.CurrentVersion));
            Assert.That(main["SavedAt"].Value<string>(), Is.Not.Empty);
            Assert.That(main["BuildVersion"].Value<string>(), Is.EqualTo("test-build"));
            Assert.That(
                (string)main["Sections"]["party"]["members"][0],
                Is.EqualTo(expectedFirstMember));
            Assert.That(Directory.GetFiles(store.DirectoryPath, "*.tmp"), Is.Empty);
        }
    }
}
