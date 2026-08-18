using System;

namespace Mochi.Event
{
    public abstract class EventBase
    {
        public readonly object Sender;

        public EventBase(object sender)
        {
            this.Sender = sender;
        }
    }

    /// <summary>
    /// 全局事件需要继承此类
    /// </summary>
    public abstract class GlobalEventBase : EventBase
    {
        protected GlobalEventBase(object sender) : base(sender)
        {

        }
    }

    /// <summary>
    /// GlobalEvent的拓展方法
    /// </summary>
    public static class GlobalEventExtension
    {
        public static void Register<T>(this T evt, Action<T> action) where T : GlobalEventBase
        {
            GlobalEventManager.Register(action);
        }

        public static void UnRegister<T>(this T evt, Action<T> action) where T : GlobalEventBase
        {
            GlobalEventManager.UnRegister(action);
        }

        public static void Publish<T>(this T evt) where T : GlobalEventBase
        {
            GlobalEventManager.Publish(evt);
        }
    }
}

