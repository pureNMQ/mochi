using System;
using System.Collections;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public abstract class HierarchyState : IState
    {
        public readonly HierarchyStateMachine Machine;
        public readonly HierarchyState Parent;
        public HierarchyState ActiveChild;

        private Func<HierarchyState> _transitionStateHandler;

        public HierarchyState(HierarchyState parent = null, Func<HierarchyState> transitionStateHandler = null)
        {
            this.Parent = parent;
            _transitionStateHandler = transitionStateHandler;
        }

        protected virtual HierarchyState InitialState => null;

        //TODO 继承关系存在问题
        IStateMachine IState.Machine { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        //生命周期函数
        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate(float deltaTime) { }

        public void Enter()
        {
            if (Parent != null) Parent.ActiveChild = this;

            OnEnter();

            if (InitialState != null) InitialState.Enter();
        }

        public void Exit()
        {
            if (ActiveChild != null)
            {
                ActiveChild.Exit();
                ActiveChild = null;
            }

            OnExit();
        }

        public void Tick(float deltaTime)
        {
            HierarchyState transitionState = GetTransitionState();
            if (transitionState != null)
            {
                Machine.Transitions.RequestTransition(this, transitionState);
                return;
            }

            if (ActiveChild != null) ActiveChild.Tick(deltaTime);

            OnUpdate(deltaTime);
        }

        private HierarchyState GetTransitionState()
        {
            return _transitionStateHandler?.Invoke();
        }

        public HierarchyState Leaf()
        {
            HierarchyState s = this;
            while (s.ActiveChild != null)
            {
                s = s.ActiveChild;
            }

            return s;
        }

        public IEnumerable<HierarchyState> PathToRoot()
        {
            for (HierarchyState s = this; s != null; s = s.Parent)
            {
                yield return s;
            }
        }

        public void SetTransition(Func<HierarchyState> stateHandler)
        {
            _transitionStateHandler = stateHandler;
        }

    }
}