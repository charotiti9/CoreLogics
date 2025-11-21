using UnityEngine;

public class StateMachineExample : MonoBehaviour
{
    private StateMachine<ExampleContext> stateMachine;

    private void Awake()
    {
        var context = new ExampleContext { Value = 0 };
        stateMachine = new StateMachine<ExampleContext>(context);
        stateMachine.ChangeState(new StateA());
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }

    // 상태 전환 예시
    public void ChangeToStateB()
    {
        stateMachine.ChangeState(new StateB());
    }
}

public class CustomStateMachineExample : MonoBehaviour
{
    private CustomStateMachine stateMachine;

    private void Awake()
    {
        var context = new ExampleContext { Value = 0 };

        stateMachine = new CustomStateMachine(context);
        stateMachine.ChangeState(new StateA());
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}

public class CustomStateMachine : StateMachine<ExampleContext>
{
    public CustomStateMachine(ExampleContext context) : base(context) { }

    // OnNext 활용 예시
    protected override void OnNext()
    {
        // 상태 전환마다 실행될 커스텀 로직
        Debug.Log($"[CustomStateMachine] 상태 전환: {PreviousState?.GetType().Name} → {CurrentState?.GetType().Name}");
    }
}

#region Context
public class ExampleContext
{
    public int Value { get; set; }
}

#endregion

#region States

/// <summary>
/// StateBase 상속 방식 (간단한 상태)
/// </summary>
public class StateA : StateBase<ExampleContext>
{
    protected override void OnEnter()
    {
        Debug.Log("StateA 진입");
        Context.Value = 1;
    }

    protected override void OnUpdate(float deltaTime)
    {
        // 업데이트 로직
    }

    protected override void OnExit()
    {
        Debug.Log("StateA 종료");
    }
}

/// <summary>
/// IState 직접 구현 방식 (복잡한 상태)
/// </summary>
public class StateB : IState<ExampleContext>
{
    public void Enter(ExampleContext context)
    {
        Debug.Log("StateB 진입");
        context.Value = 2;
    }

    public void Update(ExampleContext context, float deltaTime)
    {
        // 업데이트 로직
    }

    public void Exit(ExampleContext context)
    {
        Debug.Log("StateB 종료");
    }
}

#endregion
