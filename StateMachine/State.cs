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

        // 非 StateMachine 实现属于误用：转换失败立即暴露，静默 null 会让后续 Tick 行为不可解释
        IStateMachine IState.Machine { get => Machine; set => Machine = (StateMachine)value; }

        public State()
        {

        }

        public State(StateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        public virtual bool CanEnter() => true;

        public virtual bool CanExit() => true;

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
