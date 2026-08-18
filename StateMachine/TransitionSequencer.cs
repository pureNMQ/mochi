using System.Collections.Generic;

namespace Mochi.FSM
{
    public class TransitionSequencer
    {
        public readonly HierarchyStateMachine Machine;

        public TransitionSequencer(HierarchyStateMachine machine)
        {
            this.Machine = machine;
        }

        public void RequestTransition(HierarchyState from, HierarchyState to)
        {
            Machine.ChangeState(from, to);
        }

        public static HierarchyState Lca(HierarchyState a, HierarchyState b)
        {
            HashSet<HierarchyState> ap = new HashSet<HierarchyState>();

            for (HierarchyState s = a; s != null; s = s.Parent)
            {
                ap.Add(s);
            }

            for (HierarchyState s = b; s != null; s = s.Parent)
            {
                if (ap.Contains(s))
                {
                    return s;
                }
            }

            return null;
        }
    }
}