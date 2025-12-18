using Core.Utilities;
using UnityEngine;

public class StateMachineExample : MonoBehaviour
{
    private StateMachine<ExampleContext> stateMachine;

    private void Awake()
    {
        var context = new ExampleContext { Value = 0 };
        stateMachine = new StateMachine<ExampleContext>(context);
        stateMachine.ChangeState(new State());
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}

#region Context
public class ExampleContext
{
    public int Value { get; set; }
}

#endregion

#region States

public class State : IState<ExampleContext>
{
    public void Enter(ExampleContext context)
    {
        GameLogger.Log("State 진입");
        context.Value = 2;
    }

    public void Update(ExampleContext context, float deltaTime)
    {
        // 업데이트 로직
    }

    public void Exit(ExampleContext context)
    {
        GameLogger.Log("State 종료");
    }
}

#endregion
