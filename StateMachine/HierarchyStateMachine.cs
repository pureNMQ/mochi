using System.Collections;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public class HierarchyStateMachine : IStateMachine
    {
        public readonly HierarchyState Root;
        public readonly TransitionSequencer Transitions;

        private bool started = false;

        internal HierarchyStateMachine(HierarchyState root)
        {
            this.Root = root;
            this.Transitions = new TransitionSequencer(this);
        }

        public void Start()
        {
            if (started) return;
            Root.Enter();
            started = true;
        }

        public void Tick(float deltaTime)
        {
            if (!started) Start();
            InternalTick(deltaTime);
        }

        public void ChangeState(HierarchyState from, HierarchyState to)
        {
            if (from == to || from == null || to == null) return;

            HierarchyState lca = TransitionSequencer.Lca(from, to);

            //将from状态退出到lca状态
            for (HierarchyState s = from; s != lca; s = s.Parent)
            {
                s.Exit();
            }

            //将lca状态进入to状态
            Stack<HierarchyState> stack = new Stack<HierarchyState>();
            for (HierarchyState s = to; s != lca; s = s.Parent)
            {
                stack.Push(s);
            }

            while (stack.Count > 0)
            {
                stack.Pop().Enter();
            }
        }


        internal void InternalTick(float deltaTime)
        {
            Root.Tick(deltaTime);
        }

        public virtual void Dispose()
        {

        }

        //TODO 继承关系存在问题

        public void Start<T>(string name = "") where T : IState
        {
            throw new System.NotImplementedException();
        }

        public void Start(TypeAndName key)
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        public void ChangeState(TypeAndName typeAndName)
        {
            throw new System.NotImplementedException();
        }

        public void AddState(IState state, string name = "")
        {
            throw new System.NotImplementedException();
        }
    }
}

