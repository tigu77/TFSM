using System.Collections;
using UnityEngine;

namespace TFSM.Example
{
    //예제 상태
    public enum ESomeState
    {
        SomeState1,
        SomeState2,
        SomeState3,
    }


    //예제 상태구현의 대한 베이스 클래스
    public abstract class SomeStateBase : BaseState<ESomeState, StateMachineExample>
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

            StateMachine.ChangeState(ESomeState.SomeState2);
        }
    }

    public class SomeState2 : SomeStateBase
    {
        protected override IEnumerator OnUpdate()
        {
            yield return new WaitForSeconds(1f);

            StateMachine.ChangeState(ESomeState.SomeState3);
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
                StateMachine.ChangeState(Random.Range(0, 2) == 0 ? ESomeState.SomeState1 : ESomeState.SomeState2);
            }            
        }
    }


    public class StateMachineExample : MonoBehaviour
    {        
        private StateMachine<ESomeState, SomeStateBase, StateMachineExample> _stateMachine;

        private void Awake()
        {            
            _stateMachine = new StateMachine<ESomeState, SomeStateBase, StateMachineExample>(this);
            _stateMachine.AddState<SomeState1>(ESomeState.SomeState1);
            _stateMachine.AddState<SomeState2>(ESomeState.SomeState2);
            _stateMachine.AddState<SomeState3>(ESomeState.SomeState3);

            //상태 변경에 대한 예외 처리가 필요한 부분은 콜백으로 구현
            _stateMachine.TryChangeState = (current, next) =>
            {
                return true;
            };
        }

        private void Start()
        {
            _stateMachine.StartState(ESomeState.SomeState1);
        }

        private void Update()
        {
            //현재 상태의 구현체에 바로 접근
            _stateMachine.CurrentImplement.UpdateExternal();
        }
    }
}
