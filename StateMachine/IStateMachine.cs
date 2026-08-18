using System;
using System.Collections;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public interface IStateMachine : IDisposable
    {
        public void Start<T>(string name = "") where T : IState;
        public void Start(TypeAndName key);
        public void Tick(float deltaTime);
        public void Stop();
        public void ChangeState(TypeAndName typeAndName);
        public void AddState(IState state, string name = "");
    }
}
