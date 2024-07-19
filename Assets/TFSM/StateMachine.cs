using System;
using System.Collections.Generic;

namespace TFSM
{
    /// <summary>
    /// Interface for defining a state.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    public interface IState<State> where State : IEquatable<State>
    {
        /// <summary>
        /// Method to be called when entering the state.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Method to be called when exiting the state.
        /// </summary>
        void OnExit();
    }

    /// <summary>
    /// Interface for defining a state machine.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    public interface IStateMachine<State> where State : IEquatable<State>
    {
        /// <summary>
        /// Gets the current state.
        /// </summary>
        State Current { get; }

        /// <summary>
        /// Changes the current state to the specified state.
        /// </summary>
        /// <param name="state">The state to change to.</param>
        void ChangeState(State state);
    }

    /// <summary>
    /// Abstract base class for states.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    public abstract class BaseState<State> : IState<State> where State : IEquatable<State>
    {
        /// <summary>
        /// Gets the state machine associated with this state.
        /// </summary>
        protected IStateMachine<State> StateMachine { get; private set; }

        /// <summary>
        /// Sets the state machine for this state.
        /// </summary>
        /// <param name="stateMachine">The state machine.</param>
        internal void SetStateMachine(IStateMachine<State> stateMachine)
        {
            StateMachine = stateMachine;
        }

        /// <inheritdoc />
        public abstract void OnEnter();

        /// <inheritdoc />
        public abstract void OnExit();
    }

    /// <summary>
    /// Exception class for state machine errors.
    /// </summary>
    public class StateMachineException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public StateMachineException(string message) : base(message) { }
    }

    /// <summary>
    /// Generic state machine class.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    /// <typeparam name="StateImplement">The type of the state implementation.</typeparam>
    public class StateMachine<State, StateImplement> : IStateMachine<State>
        where State : IEquatable<State>
        where StateImplement : BaseState<State>
    {
        /// <summary>
        /// Delegate for trying to change the state.
        /// </summary>
        /// <param name="current">The current state.</param>
        /// <param name="next">The next state.</param>
        /// <returns>True if the state change is allowed; otherwise, false.</returns>
        public delegate bool TryChangeStateCallback(State current, State next);

        /// <summary>
        /// Represents state information.
        /// </summary>
        private class StateInfo
        {
            public State state;
            public StateImplement implement;
        }

        private readonly Dictionary<State, StateInfo> _states = new Dictionary<State, StateInfo>();
        private StateInfo _current;

        /// <summary>
        /// Gets a value indicating whether the state machine is in progress.
        /// </summary>
        public bool IsProgress => _current != null;

        /// <summary>
        /// Gets the current state of the state machine.
        /// </summary>
        public State Current => _current.state;

        /// <summary>
        /// Gets the current state implementation.
        /// </summary>
        public StateImplement CurrentImplement => _current.implement;

        /// <summary>
        /// Gets or sets the callback for trying to change the state.
        /// </summary>
        public TryChangeStateCallback TryChangeState { get; set; }

        /// <summary>
        /// Event that is triggered when the state is changed.
        /// </summary>
        public event Action<State, State> OnStateChanged;

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <param name="state">The state to add.</param>
        /// <param name="stateImplement">The state implementation.</param>
        /// <exception cref="StateMachineException">Thrown if the state already exists.</exception>
        public void AddState(State state, StateImplement stateImplement)
        {
            if (_states.ContainsKey(state))
            {
                throw new StateMachineException("State already exists: " + state);
            }

            stateImplement.SetStateMachine(this);
            _states.Add(state, new StateInfo
            {
                state = state,
                implement = stateImplement
            });
        }

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <typeparam name="T">The type of the state implementation.</typeparam>
        /// <param name="state">The state to add.</param>
        public void AddState<T>(State state) where T : StateImplement, new()
        {
            AddState(state, new T());
        }

        /// <summary>
        /// Clears the current state.
        /// </summary>
        public void ClearState()
        {
            if (_current != null)
            {
                _current.implement.OnExit();
                _current = null;
            }
        }

        /// <summary>
        /// Starts the state machine with the specified state.
        /// </summary>
        /// <param name="state">The state to start with.</param>
        /// <exception cref="StateMachineException">Thrown if the state machine is already in progress.</exception>
        public void StartState(State state)
        {
            if (_current != null)
            {
                throw new StateMachineException("State machine already in progress");
            }

            _current = GetStateInfo(state);
            _current.implement.OnEnter();
        }

        /// <inheritdoc />
        /// <exception cref="StateMachineException">Thrown if the state machine is not started.</exception>
        public void ChangeState(State state)
        {
            if (_current == null)
            {
                throw new StateMachineException("State machine not started");
            }

            if (_current.state.Equals(state))
            {
                return;
            }

            if (TryChangeState != null && !TryChangeState(_current.state, state))
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

        /// <summary>
        /// Gets the state information for the specified state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>The state information.</returns>
        /// <exception cref="StateMachineException">Thrown if the state does not exist.</exception>
        private StateInfo GetStateInfo(State state)
        {
            if (!_states.TryGetValue(state, out StateInfo stateInfo))
            {
                throw new StateMachineException("State does not exist: " + state);
            }

            return stateInfo;
        }
    }

    /// <summary>
    /// Abstract base class for states with context.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    /// <typeparam name="ContextType">The type of the context.</typeparam>
    public abstract class BaseState<State, ContextType> : BaseState<State> where State : IEquatable<State>
    {
        /// <summary>
        /// Gets the context associated with this state.
        /// </summary>
        protected ContextType Context { get; private set; }

        /// <summary>
        /// Sets the context for this state.
        /// </summary>
        /// <param name="context">The context.</param>
        internal void SetContext(ContextType context)
        {
            Context = context;
        }
    }

    /// <summary>
    /// Generic state machine class with context.
    /// </summary>
    /// <typeparam name="State">The type of the state.</typeparam>
    /// <typeparam name="StateImplement">The type of the state implementation.</typeparam>
    /// <typeparam name="ContextType">The type of the context.</typeparam>
    public class StateMachine<State, StateImplement, ContextType> : StateMachine<State, StateImplement>
        where State : IEquatable<State>
        where StateImplement : BaseState<State, ContextType>
    {
        private readonly ContextType _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachine{State, StateImplement, ContextType}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public StateMachine(ContextType context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <param name="state">The state to add.</param>
        /// <param name="stateImplement">The state implementation.</param>
        public new void AddState(State state, StateImplement stateImplement)
        {
            stateImplement.SetContext(_context);
            base.AddState(state, stateImplement);
        }

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <typeparam name="T">The type of the state implementation.</typeparam>
        /// <param name="state">The state to add.</param>
        public new void AddState<T>(State state) where T : StateImplement, new()
        {
            AddState(state, new T());
        }
    }
}
