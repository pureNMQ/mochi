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
    }
}
