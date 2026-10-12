namespace Mochi.Save
{
    /// <summary>
    /// 可存档系统与存档框架的契约:一个实现对应档文件里的一个节。
    /// 存与读由同一实现负责,存储形状才可能对称;实现方通常是业务服务本身,
    /// <see cref="SaveCabinet"/> 只做调度与 JSON 转换。
    /// </summary>
    /// <typeparam name="TData">该节的存档数据形状;空状态用空 DTO 表达。</typeparam>
    public interface ISaveSection<TData>
    {
        /// <summary>档文件中的节名。它是档格式的一部分,改名等同废档。</summary>
        string SectionId { get; }

        /// <summary>
        /// 取当前运行状态的快照。返回 null 会让档失去该节的数据形状,
        /// 因此空状态必须返回空 DTO,由 <see cref="SaveCabinet.CaptureAll"/> 拒绝 null。
        /// </summary>
        TData Capture();

        /// <summary>
        /// 用档中数据覆盖当前状态。<paramref name="data"/> 可能为 null(档中该节为 JSON null),实现方必须接受。
        /// </summary>
        void Restore(TData data);
    }
}
