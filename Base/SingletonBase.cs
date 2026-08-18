using System.Collections;
using System.Collections.Generic;

namespace Mochi
{
    /// <summary>
    /// 单例模式基类，
    /// 使用案例：public class EventManager : Singleton<EventManager>
    /// </summary>
    /// <typeparam name="T">子类的类型</typeparam>
    public abstract class Singleton<T> where T : new()
    {
        private static T instance;
        private static readonly object locker = new object();

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                            instance = new T();
                    }
                }

                return instance;
            }
        }
    }
}


