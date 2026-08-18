using System;
using System.Collections.Generic;

namespace Mochi.Event
{
    public class EventManager
    {
        //用于在字典中储存的接口
        interface IRegistrations
        {
        }

        //用来承载具体事件的类
        class Registrations<T> : IRegistrations
        {
            public Action<T> OnReceives;
        }

        private Dictionary<Type, IRegistrations> mTypeEventDict = new Dictionary<Type, IRegistrations>();


        /// <summary>
        /// 注册事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public void Register<T>(Action<T> onReceive) where T : EventBase
        {
            var type = typeof(T);

            IRegistrations registrations = null;

            if (mTypeEventDict.TryGetValue(type, out registrations))
            {
                var reg = registrations as Registrations<T>;
                reg.OnReceives += onReceive;
            }
            else
            {
                var reg = new Registrations<T>();
                reg.OnReceives += onReceive;
                mTypeEventDict.Add(type, reg);
            }
        }

        /// <summary>
        /// 注销事件
        /// </summary>
        /// <param name="onReceive"></param>
        /// <typeparam name="T"></typeparam>
        public void UnRegister<T>(Action<T> onReceive) where T : EventBase
        {
            var type = typeof(T);

            IRegistrations registrations = null;

            if (mTypeEventDict.TryGetValue(type, out registrations))
            {
                var reg = registrations as Registrations<T>;
                reg.OnReceives -= onReceive;
            }
        }

        /// <summary>
        /// 发送事件
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        public void Publish<T>(T t) where T : EventBase
        {
            var type = typeof(T);

            if (mTypeEventDict.TryGetValue(type, out IRegistrations registrations))
            {
                var reg = registrations as Registrations<T>;
                reg.OnReceives(t);
            }
        }
    }
}




