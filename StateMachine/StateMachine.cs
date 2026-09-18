using System;
using System.Collections.Generic;

namespace Mochi.FSM
{
    public class StateMachine : IStateMachine
    {
        protected Dictionary<TypeAndName, IState> states = new();
        private IState currentState;
        private bool isStarted = false;

        public IState CurrentState => currentState;

        /// <summary>新状态进入完成后广播 (previous, current)；起始与停止的边界侧为 null。</summary>
        public event Action<IState, IState> StateChanged;

        public void Start<T>(string name = "") where T : IState
        {
            if (isStarted) return;
            Start(new TypeAndName(typeof(T), name));
        }

        public void Start(TypeAndName key)
        {
            if (isStarted) return;
            if (!states.ContainsKey(key)) return;
            if (!states[key].CanEnter()) return;

            currentState = states[key];
            isStarted = true;
            currentState?.Enter();
            StateChanged?.Invoke(null, currentState);
        }

        public void Tick(float deltaTime)
        {
            if (!isStarted) return;
            currentState?.Tick(deltaTime);
        }

        public void Stop()
        {
            if (!isStarted) return;

            IState previous = currentState;
            currentState?.Exit();
            currentState = null;
            isStarted = false;
            StateChanged?.Invoke(previous, null);
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

        /// <summary>守卫拒绝、键未知或未启动时保持原状态，不抛异常。</summary>
        public void ChangeState(TypeAndName typeAndName)
        {
            if (!isStarted) return;
            if (!states.ContainsKey(typeAndName)) return;
            if (currentState != null && !currentState.CanExit()) return;
            if (!states[typeAndName].CanEnter()) return;

            IState previous = currentState;
            previous?.Exit();
            currentState = states[typeAndName];
            currentState?.Enter();
            StateChanged?.Invoke(previous, currentState);
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
