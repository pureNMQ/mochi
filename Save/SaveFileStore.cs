using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mochi.Logging;
using Newtonsoft.Json;

namespace Mochi.Save
{
    /// <summary>
    /// 单档的文件存储:固定路径的一个主档加一个上一版备份。
    /// 写档经同目录临时文件落盘后整体替换,主档不会出现半写;读档只负责取出完整档,
    /// 损坏后的回退选择与版本判定由上层流程负责。
    /// </summary>
    public sealed class SaveFileStore
    {
        /// <summary>存档目录名,由宿主拼到自身的数据目录下。</summary>
        public const string SaveDirectoryName = "Save";

        /// <summary>主档文件名。</summary>
        public const string MainFileName = "Save.json";

        /// <summary>上一版主档的备份文件名。</summary>
        public const string BackupFileName = "Save.json.bak";

        /// <summary>隔离文件名里插在原名之后的标记,便于按名识别迁移链材料。</summary>
        public const string QuarantineInfix = ".corrupt.";

        private const string TempFileSuffix = ".tmp";

        private readonly ILogger log;

        /// <param name="directoryPath">存档目录;不在构造期创建,由 <see cref="WriteAtomic"/> 按需创建。</param>
        /// <param name="log">日志记录器;为 null 时取 <see cref="LogManager"/> 的本类型分类默认实例。</param>
        /// <exception cref="ArgumentException">directoryPath 为 null 或空白。</exception>
        public SaveFileStore(string directoryPath, ILogger log = null)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("存档目录不能为 null 或空白。", nameof(directoryPath));
            }

            DirectoryPath = directoryPath;
            MainFilePath = Path.Combine(directoryPath, MainFileName);
            BackupFilePath = Path.Combine(directoryPath, BackupFileName);
            this.log = log ?? LogManager.GetLogger(nameof(SaveFileStore));
        }

        /// <summary>存档目录。</summary>
        public string DirectoryPath { get; }

        /// <summary>主档完整路径。</summary>
        public string MainFilePath { get; }

        /// <summary>备份档完整路径。</summary>
        public string BackupFilePath { get; }

        /// <summary>主档是否存在。</summary>
        public bool Exists => File.Exists(MainFilePath);

        /// <summary>备份档是否存在。</summary>
        public bool BackupExists => File.Exists(BackupFilePath);

        /// <summary>
        /// 读主档。文件不存在、JSON 不可解析或形状不是档对象时记录告警并返回 null,不抛异常:
        /// 存档是磁盘上的外部输入,解析期任何异常都只等价于读不出来,不能冒到调用方。
        /// 主档读不出来时是否回退到备份由上层决定。
        /// </summary>
        /// <returns>可用的档;读不出来时为 null。</returns>
        public SaveDocument Read()
        {
            if (!Exists)
            {
                return null;
            }

            return ReadFile(MainFilePath);
        }

        /// <summary>
        /// 读备份档。与 <see cref="Read"/> 同口径:文件不存在、JSON 不可解析或形状非法时记录告警并返回 null,不抛异常。
        /// 主档与备份的取舍由上层决定(见 <see cref="SaveVersionGuard"/>)。
        /// </summary>
        /// <returns>可用的备份档;读不出来时为 null。</returns>
        public SaveDocument ReadBackup()
        {
            if (!BackupExists)
            {
                return null;
            }

            return ReadFile(BackupFilePath);
        }

        private SaveDocument ReadFile(string filePath)
        {
            string json;
            try
            {
                json = File.ReadAllText(filePath, new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                log.WarnFormat("读取存档文件失败:{0}", exception.Message);
                return null;
            }

            try
            {
                SaveDocument document = JsonConvert.DeserializeObject<SaveDocument>(
                    json,
                    SaveJson.CreateSettings());
                if (document == null)
                {
                    log.WarnFormat("存档内容为 JSON null,视为不可读:{0}", Path.GetFileName(filePath));
                    return null;
                }
                return document;
            }
            catch (Exception exception)
            {
                log.WarnFormat("存档 JSON 无法解析或形状非法:{0}", exception.Message);
                return null;
            }
        }

        /// <summary>
        /// 原子写档:先把完整 JSON 写入同目录临时文件并 Flush 到磁盘,再整体替换主档,
        /// 因此主档要么是旧内容要么是新内容。主档已存在时旧内容同时成为备份档,即上一版主档。
        /// </summary>
        /// <param name="document">要落盘的档。</param>
        /// <exception cref="ArgumentNullException">document 为 null。</exception>
        public void WriteAtomic(SaveDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Directory.CreateDirectory(DirectoryPath);
            string json = JsonConvert.SerializeObject(document, SaveJson.CreateSettings());
            string tempFilePath = MainFilePath + TempFileSuffix;

            try
            {
                using (var stream = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (Exists)
                {
                    File.Replace(tempFilePath, MainFilePath, BackupFilePath);
                    return;
                }
                File.Move(tempFilePath, MainFilePath);
            }
            catch
            {
                // 写失败的半成品不留在存档目录,否则它会一直作为异常文件呈现给存档管理窗口
                TryDeleteFile(tempFilePath);
                throw;
            }
        }

        /// <summary>
        /// 把主档覆盖式移到备份位置:先写备份再删主档,复制失败时主档保持原样。
        /// 这是显式备份动作,成功后主档不存在;写档路径不依赖它,<see cref="WriteAtomic"/> 自身维护备份。
        /// </summary>
        /// <returns>主档存在并完成移动时为 true,主档不存在时为 false。</returns>
        public bool Backup()
        {
            if (!Exists)
            {
                return false;
            }

            File.Copy(MainFilePath, BackupFilePath, true);
            File.Delete(MainFilePath);
            return true;
        }

        /// <summary>
        /// 把主档改名隔离为 Save.json.corrupt.yyyyMMddHHmmss(UTC),内容原样保留,
        /// 供版本不符等"不许再读但不销毁恢复材料"的处置使用。
        /// </summary>
        /// <returns>隔离后的文件名;主档不存在时为 null。</returns>
        public string Quarantine()
        {
            if (!Exists)
            {
                return null;
            }

            string fileName = MainFileName + QuarantineInfix + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Move(MainFilePath, Path.Combine(DirectoryPath, fileName));
            return fileName;
        }

        /// <summary>
        /// 把主档与备份档<strong>成对</strong>改名隔离,两份内容都原样保留。
        /// 版本不符是主档与 .bak 的共同状态,只挪主档会让下一版代码把那个同样过新的 .bak 当成可用档读回来。
        /// </summary>
        /// <returns>实际移动的文件名,主档在前;两份都不存在时为空数组。</returns>
        public string[] QuarantinePair()
        {
            var moved = new List<string>();
            string stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            if (Exists)
            {
                string fileName = MainFileName + QuarantineInfix + stamp;
                File.Move(MainFilePath, Path.Combine(DirectoryPath, fileName));
                moved.Add(fileName);
            }

            if (BackupExists)
            {
                string fileName = BackupFileName + QuarantineInfix + stamp;
                File.Move(BackupFilePath, Path.Combine(DirectoryPath, fileName));
                moved.Add(fileName);
            }

            return moved.ToArray();
        }

        /// <summary>
        /// 删除主档与备份档,用于编辑器清档。隔离文件是刻意留存的恢复材料,不在此列:
        /// 存档管理窗口把它单独列出并单独删除,因此清档不会隐式销毁迁移材料。
        /// </summary>
        public void DeleteAll()
        {
            TryDeleteFile(MainFilePath);
            TryDeleteFile(BackupFilePath);
        }

        /// <summary>
        /// 把主档内容换成<strong>确实解析不出</strong>的片段,供损坏路径的故障注入使用。
        /// 不能用"看起来像坏 JSON"的截断对象(如 <c>{"corrupt":</c>):Newtonsoft 会把它解析成
        /// 一个字段全默认的合法 SaveDocument,于是"损坏"变成"可读的空档",故障注入就注不出故障了。
        /// 主档不存在时不产生任何文件,避免注入动作凭空造出一个假档。
        /// </summary>
        public void InjectCorruption()
        {
            if (!Exists)
            {
                log.Warn("主档不存在,未注入损坏。");
                return;
            }

            File.WriteAllText(MainFilePath, "corrupted-by-injection", new UTF8Encoding(false));
        }

        private static void TryDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
