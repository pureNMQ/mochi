namespace Mochi.ServiceLocator
{
    /// <summary>
    /// 服务定位器的静态入口。组合根装配完成后把官方容器赋给 Root，
    /// 供无法经构造注入拿到容器的调用方取服务。Root 是便利指针而非系统依赖：
    /// 可重复指向或置 null，容器自身行为与系统正确性都不依赖它。
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// 组合根注册的官方容器；未赋值时为 null，调用方取服务前须自行判空。
        /// </summary>
        public static ServiceProvider Root { get; set; }
    }
}
