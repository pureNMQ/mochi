using System;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public class StateMachine : IStateMachine
    {
        protected Dictionary<TypeAndName, IState> states = new();
        private IState currentState;
        private bool isStarted = false;
        private TypeAndName startState;
        public IState CurrentState => currentState;

        public void Start<T>(string name = "") where T : IState
        {
            if (isStarted) return;
            Start(new TypeAndName(typeof(T), name));
        }

        public void Start(TypeAndName key)
        {
            if (isStarted) return;
            if (!states.ContainsKey(key)) return;
            currentState = states[key];
            isStarted = true;
            currentState?.Enter();
        }

        public void Tick(float deltaTime)
        {
            if (!isStarted) return;
            currentState?.Tick(deltaTime);
        }

        public void Stop()
        {
            if (!isStarted) return;

            currentState?.Exit();
            isStarted = false;
        }

        public void Dispose()
        {
            foreach (var state in states.Values)
            {
                IDisposable disposable = state as IDisposable;
                disposable?.Dispose();
            }
        }

        public IState GetState(TypeAndName typeAndName)
        {
            if (!states.ContainsKey(typeAndName)) return null;

            return states[typeAndName];
        }

        public IState GetState<T>(string name = "") where T : IState
        {
            return GetState(new TypeAndName(typeof(T), name));
        }

        public void ChangeState(TypeAndName typeAndName)
        {
            if (!states.ContainsKey(typeAndName))
            {
                return;
            }
            currentState?.Exit();
            currentState = states[typeAndName];
            currentState?.Enter();
        }

        public void ChangeState<T>(string name = "") where T : IState
        {
            ChangeState(new TypeAndName(typeof(T), name));
        }

        public void AddState(IState state, string name = "")
        {
            state.Machine = this;
            TypeAndName key = new TypeAndName(state.GetType(), name);
            if (!states.ContainsKey(key))
            {
                states.Add(key, state);
            }
            else if (currentState != states[key])
            {
                states[key] = state;
            }
        }

    }
}
