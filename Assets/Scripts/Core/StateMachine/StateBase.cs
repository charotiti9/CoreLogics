using System;

/// <summary>
/// 상태 베이스 클래스 (선택적 사용)
/// IState를 직접 구현하는 대신 이 클래스를 상속하여 간단하게 사용 가능
/// Context를 protected 필드로 제공하여 파생 클래스에서 쉽게 접근 가능
/// </summary>
/// <typeparam name="TContext">상태가 조작할 컨텍스트 타입 (class 제약)</typeparam>
public abstract class StateBase<TContext> : IState<TContext> where TContext : class
{
    /// <summary>
    /// 상태가 조작할 컨텍스트 객체
    /// Enter에서 설정되고 Exit에서 null로 초기화됨
    /// </summary>
    protected TContext Context { get; private set; }

    [Obsolete("StateMachine에서 호출하는 함수입니다. 직접 호출하지 마세요.")]
    public void Enter(TContext context)
    {
        Context = context;
        OnEnter();
    }

    [Obsolete("StateMachine에서 호출하는 함수입니다. 직접 호출하지 마세요.")]
    public void Update(TContext context, float deltaTime)
    {
        OnUpdate(deltaTime);
    }

    [Obsolete("StateMachine에서 호출하는 함수입니다. 직접 호출하지 마세요.")]
    public void Exit(TContext context)
    {
        OnExit();
        Context = null;
    }

    /// <summary>
    /// 상태 진입 시 호출되는 가상 메서드
    /// 파생 클래스에서 필요 시 오버라이드
    /// </summary>
    protected virtual void OnEnter() { }

    /// <summary>
    /// 매 프레임 호출되는 가상 메서드
    /// 파생 클래스에서 필요 시 오버라이드
    /// </summary>
    /// <param name="deltaTime">이전 프레임으로부터 경과된 시간</param>
    protected virtual void OnUpdate(float deltaTime) { }

    /// <summary>
    /// 상태 종료 시 호출되는 가상 메서드
    /// 파생 클래스에서 필요 시 오버라이드
    /// </summary>
    protected virtual void OnExit() { }
}
