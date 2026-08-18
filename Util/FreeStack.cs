using System;
using System.Collections.Generic;

namespace Mochi
{
    public class FreeStack<T>
    {
        LinkedList<T> list = new LinkedList<T>();
        Dictionary<T, LinkedListNode<T>> dic = new Dictionary<T, LinkedListNode<T>>();
        public int Count => list.Count;

        public void Push(T item)
        {
            list.AddLast(item);
            dic[item] = list.Last;
        }

        public T Pop()
        {
            if (Count == 0) return default;
            var item = list.Last.Value;
            list.RemoveLast();
            dic.Remove(item);
            return item;
        }

        public T Peek()
        {
            if (Count == 0) return default;
            return list.Last.Value;
        }

        public T Remove(T item)
        {
            if (!Contains(item)) return default;
            var node = dic[item];
            list.Remove(node);
            dic.Remove(item);
            return item;
        }

        public bool Contains(T item)
        {
            return dic.ContainsKey(item);
        }

        public void Clear()
        {
            list.Clear();
            dic.Clear();
        }

    }
}