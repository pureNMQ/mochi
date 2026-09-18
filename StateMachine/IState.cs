using System.Collections;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public interface IState
    {
        public IStateMachine Machine { get; set; }
        public void Enter();
        public void Exit();
        public void Tick(float deltaTime);

        /// <summary>状态机尝试进入本状态前询问；返回 false 则拒绝切换。不得产生副作用。</summary>
        bool CanEnter() => true;

        /// <summary>状态机尝试离开本状态前询问；返回 false 则拒绝切换。不得产生副作用。</summary>
        bool CanExit() => true;
    }
}
