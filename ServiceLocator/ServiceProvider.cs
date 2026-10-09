using System;
using System.Collections.Generic;

namespace Mochi.ServiceLocator
{
    /// <summary>
    /// 按类型注册与获取服务的实例容器。每个容器持有独立注册表，实例间互不共享；
    /// 注册只发生在组合根装配阶段，取用阶段只读。
    /// </summary>
    public class ServiceProvider
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        /// <summary>
        /// 以类型 T 注册服务实例。同一类型重复注册会抛出异常，已注册实例不被覆盖。
        /// </summary>
        /// <typeparam name="T">服务的注册类型；获取方必须以同一类型调用 <see cref="Get{T}"/>。</typeparam>
        /// <param name="instance">服务实例。</param>
        /// <exception cref="ArgumentNullException">instance 为 null。</exception>
        /// <exception cref="InvalidOperationException">类型 T 已注册。</exception>
        public void Register<T>(T instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type key = typeof(T);
            if (services.ContainsKey(key))
                throw new InvalidOperationException(
                    $"Service of type {key.FullName} is already registered.");

            services.Add(key, instance);
        }

        /// <summary>
        /// 获取以类型 T 注册的服务实例。类型必须与注册时传入的类型完全一致，
        /// 以基类或接口注册的服务须以同一基类或接口获取。
        /// </summary>
        /// <typeparam name="T">注册时使用的类型。</typeparam>
        /// <exception cref="InvalidOperationException">类型 T 未注册。</exception>
        public T Get<T>()
        {
            Type key = typeof(T);
            if (!services.TryGetValue(key, out object service))
                throw new InvalidOperationException(
                    $"Service of type {key.FullName} is not registered.");

            return (T)service;
        }
    }
}
