using System;
using System.Collections.Generic;

namespace TFSM
{
    /// <summary>
    /// State Interface
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    public interface IState<State>
    {
        void OnEnter();
        void OnExit();
    }


    /// <summary>
    /// State Machine Interface
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    public interface IStateMachine<State>
    {
        State Current { get; }
        void ChangeState(State state);
    }


    /// <summary>
    /// Generic State
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    public abstract class BaseState<State> : IState<State>
    {
        protected IStateMachine<State> StateMachine { get; private set; }
        internal void SetStateMachine(IStateMachine<State> stateMachine)
        {
            StateMachine = stateMachine;
        }

        public abstract void OnEnter();
        public abstract void OnExit();
    }


    /// <summary>
    /// State Machine Exception
    /// </summary>
    public class StateMachineException : Exception
    {
        public StateMachineException(string message) :
            base(message)
        {
        }
    }


    /// <summary>
    /// Generic State Machine
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    /// <typeparam name="StateImplement">State Implement Type</typeparam>
    public class StateMachine<State, StateImplement> : IStateMachine<State>
        where StateImplement : BaseState<State>
    {
        public delegate bool TryChangeStateCallback(State current, State next);

        private class StateInfo
        {
            public State state;
            public StateImplement implement;
        }

        private readonly Dictionary<State, StateInfo> _states = new Dictionary<State, StateInfo>();
        private StateInfo _current;
        
        public bool IsProgress => _current != null;
        public State Current => _current.state;
        public StateImplement CurrentImplement => _current.implement;

        public TryChangeStateCallback TryChangeState { get; set; }

        public event Action<State, State> OnStateChanged;
        
                
        public void AddState(State state, StateImplement stateImplment)
        {
            stateImplment.SetStateMachine(this);
            _states.Add(state, new StateInfo
            {
                state = state,
                implement = stateImplment
            });
        }

        public void AddState<T>(State state) where T : StateImplement, new()
        {
            AddState(state, new T());
        }

        public void ClearState()
        {
            if(_current != null)
            {
                _current.implement.OnExit();
                _current = null;
            }
        }

        public void StartState(State state)
        {
            if (_current != null)
            {
                throw new StateMachineException("already progress state machine");
            }

            _current = GetStateInfo(state);
            _current.implement.OnEnter();
        }

        public void ChangeState(State state)
        {
            if(_current == null)
            {
                throw new StateMachineException("not started state machine");
            }

            if(_current.state.Equals(state))
            {
                return;
            }

            if(TryChangeState != null && !TryChangeState(_current.state, state))
            {
                return;
            }

            var stateInfo = GetStateInfo(state);
            var prev = _current;
            _current.implement.OnExit();
            _current = stateInfo;
            _current.implement.OnEnter();
            OnStateChanged?.Invoke(prev.state, state);
        }

        private StateInfo GetStateInfo(State state)
        {
            if (!_states.TryGetValue(state, out StateInfo stateInfo))
            {
                throw new StateMachineException("not exist state - " + state);
            }

            return stateInfo;
        }
    }


    /// <summary>
    /// Context based State
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    /// <typeparam name="ContextType">Context Type</typeparam>
    public abstract class BaseState<State, ContextType> : BaseState<State>
    {
        protected ContextType Context { get; private set; }

        internal void SetContext(ContextType context)
        {
            Context = context;
        }

    }



    /// <summary>
    /// Context based State Machine
    /// </summary>
    /// <typeparam name="State">State Type</typeparam>
    /// <typeparam name="StateImplement">State Implement</typeparam>
    /// <typeparam name="ContextType">Context Type</typeparam>
    public class StateMachine<State, StateImplement, ContextType> : StateMachine<State, StateImplement>
        where StateImplement : BaseState<State, ContextType>
    {
        private readonly ContextType _context;

        public StateMachine(ContextType context)            
        {
            _context = context;
        }

        public new void AddState(State state, StateImplement stateImplement)
        {
            stateImplement.SetContext(_context);
            base.AddState(state, stateImplement);
        }

        public new void AddState<T>(State state) where T : StateImplement, new()
        {
            AddState(state, new T());
        }
    }

}
