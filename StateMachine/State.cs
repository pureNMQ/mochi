using System.Collections;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public abstract class State : IState
    {
        private bool isEntered = false;
        private StateMachine stateMachine;

        public bool IsEntered => isEntered;

        public StateMachine Machine
        {
            get => stateMachine;
            set => stateMachine = value;
        }
        //TODO 接口未实现
        IStateMachine IState.Machine { get => Machine; set => throw new System.NotImplementedException(); }

        public State()
        {

        }

        public State(StateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public void Enter()
        {
            if (isEntered) return;

            OnEnter();
            isEntered = true;
        }

        public void Exit()
        {
            if (!isEntered) return;

            OnExit();
            isEntered = false;
        }

        public void Tick(float deltaTime)
        {
            if (!isEntered)
            {
                return;
            }

            OnTick(deltaTime);
        }

        protected abstract void OnEnter();
        protected abstract void OnExit();
        protected abstract void OnTick(float deltaTime);

    }
}
