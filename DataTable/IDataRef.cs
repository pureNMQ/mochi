namespace Mochi.DataTable
{
    /// <summary>
    /// 数据表行引用的标记接口:实现者只持有目标行的 string id,首次访问时才解析为行对象,
    /// 装载期不做任何引用物化,循环引用的数据因此不会递归装载。
    /// </summary>
    public interface IDataRef<TRow> where TRow : TableRow
    {
    }
}
