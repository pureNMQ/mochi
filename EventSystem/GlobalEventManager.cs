using System;

namespace Mochi.Event
{
    /// <summary>
    /// 全局事件管理器，用于管理全局事件
    /// </summary>
    public class GlobalEventManager : Singleton<GlobalEventManager>
    {
        protected EventManager eventManager;

        public GlobalEventManager()
        {
            eventManager = new EventManager();
        }

        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(Action<T> onReceive) where T : GlobalEventBase
        {
            Instance.eventManager.Register(onReceive);
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public static void UnRegister<T>(Action<T> onReceive) where T : GlobalEventBase
        {
            Instance.eventManager.UnRegister(onReceive);
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        public static void Publish<T>(T t) where T : GlobalEventBase
        {
            Instance.eventManager.Publish(t);
        }
    }
}
