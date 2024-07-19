# Unity와 C#에서 사용 가능한 일반적인 상태 기계

``` C#
using System;
using System.Collections;
using UnityEngine;

namespace TFSM.Example
{
    //상태는 박싱을 막기 위해 IEquatable<T>를 구현해야 한다
    public struct SomeState : IEquatable<SomeState>
    {
        public enum Type
        {
            SomeState1,
            SomeState2,
            SomeState3
        }

        private Type type;
        

        public readonly bool Equals(SomeState other)
        {
            return type == other.type;
        }

        public static implicit operator SomeState(Type type)
        {
            return new SomeState
            {
                type = type
            };
        }
    }


    //예제 상태구현 베이스 클래스
    public abstract class SomeStateBase : BaseState<SomeState, StateMachineExample>
    {
        private Coroutine _coroutine;

        //머신에서 호출
        public override void OnEnter()
        {
            Debug.Log($"{GetType()} OnEnter");
            _coroutine = Context.StartCoroutine(OnUpdate());
        }

        //머신에서 호출
        public override void OnExit()
        {
            Debug.Log($"{GetType()} OnExit");

            if (_coroutine != null)
            {
                Context.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        //내부에서 호출
        protected virtual IEnumerator OnUpdate()
        {
            yield break;
        }

        //외부에서 호출
        public virtual void UpdateExternal()
        {
        }
    }

    public class SomeState1 : SomeStateBase
    {
        protected override IEnumerator OnUpdate()
        {
            yield return new WaitForSeconds(1f);

            StateMachine.ChangeState(SomeState.Type.SomeState2);
        }
    }

    public class SomeState2 : SomeStateBase
    {
        protected override IEnumerator OnUpdate()
        {
            yield return new WaitForSeconds(1f);

            StateMachine.ChangeState(SomeState.Type.SomeState3);
        }
    }

    public class SomeState3 : SomeStateBase
    {
        private float _endTime;
        public override void OnEnter()
        {
            base.OnEnter();
            _endTime = Time.time + 1f;
        }

        public override void UpdateExternal()
        {
            if(Time.time >= _endTime)
            {                
                StateMachine.ChangeState(UnityEngine.Random.Range(0, 2) == 0 ? SomeState.Type.SomeState1 : SomeState.Type.SomeState2);
            }            
        }
    }


    public class StateMachineExample : MonoBehaviour
    {        
        private StateMachine<SomeState, SomeStateBase, StateMachineExample> _stateMachine;

        private void Awake()
        {            
            _stateMachine = new StateMachine<SomeState, SomeStateBase, StateMachineExample>(this);
            _stateMachine.AddState<SomeState1>(SomeState.Type.SomeState1);
            _stateMachine.AddState<SomeState2>(SomeState.Type.SomeState2);
            _stateMachine.AddState<SomeState3>(SomeState.Type.SomeState3);

            //상태 변경에 대한 예외 처리가 필요한 부분은 콜백으로 구현
            _stateMachine.TryChangeState = (current, next) =>
            {
                return true;
            };
        }

        private void Start()
        {
            _stateMachine.StartState(SomeState.Type.SomeState1);
        }

        private void Update()
        {
            //현재 상태의 구현체에서 바로 호출
            _stateMachine.CurrentImplement.UpdateExternal();
        }
    }
}


```