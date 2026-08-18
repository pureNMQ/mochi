using System.Collections.Generic;
using System.Reflection;

namespace Mochi.FSM
{
    public class HierarchyStateMachineBuilder
    {
        private readonly HierarchyState _root;

        public HierarchyStateMachineBuilder(HierarchyState root)
        {
            _root = root;
        }

        public HierarchyStateMachine Build()
        {
            HierarchyStateMachine machine = new HierarchyStateMachine(_root);
            Wire(_root, machine, new HashSet<HierarchyState>());
            return machine;
        }

        //将状态机注入到所有状态中
        private void Wire(HierarchyState s, HierarchyStateMachine m, HashSet<HierarchyState> visited)
        {
            if (s == null) return;
            if (!visited.Add(s)) return;

            //NOTE 将状态机实例注入到状态中
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            var machineField = typeof(HierarchyState).GetField("Machine", flags);
            if (machineField != null) machineField.SetValue(s, m);

            //NOTE 找出该状态类型中所有派生自State的子类字段，将它们视为该状态类型的子状态
            foreach (var field in s.GetType().GetFields(flags))
            {
                if (!typeof(HierarchyState).IsAssignableFrom(field.FieldType)) continue;
                if (field.Name == "Parent") continue;

                var child = (HierarchyState)field.GetValue(s);
                if (child == null) continue;
                if (!ReferenceEquals(child.Parent, s)) continue;

                //NOTE visited用于防止循环引用
                Wire(child, m, visited);
            }
        }

    }
}