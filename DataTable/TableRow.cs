namespace Mochi.DataTable
{
    /// <summary>
    /// 数据表行类型的基类:表键恒为 <see cref="id"/>,由基类统一定义,
    /// DataTable 族与 IDataRef 族的泛型约束都指向本类,未继承本类的行类型在编译期被拒绝。
    /// </summary>
    public abstract class TableRow
    {
        /// <summary>行 id,即所在表的键;同一表内必须非空且唯一。</summary>
        public string id;

        /// <summary>id 字段在 JSON 数据文件中的字段名,供装载与编辑器侧定位错误使用。</summary>
        public const string IdFieldName = "id";
    }
}
