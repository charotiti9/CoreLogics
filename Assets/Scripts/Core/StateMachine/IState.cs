/// <summary>
/// 상태 인터페이스
/// StateMachine에서 관리되는 상태의 기본 동작을 정의
/// </summary>
/// <typeparam name="TContext">상태가 조작할 컨텍스트 타입 (class 제약)</typeparam>
public interface IState<TContext> where TContext : class
{
    /// <summary>
    /// 상태 진입 시 호출
    /// </summary>
    /// <param name="context">상태가 조작할 컨텍스트 객체</param>
    void Enter(TContext context);

    /// <summary>
    /// 매 프레임 호출 (GameFlowManager를 통해)
    /// </summary>
    /// <param name="context">상태가 조작할 컨텍스트 객체</param>
    /// <param name="deltaTime">이전 프레임으로부터 경과된 시간</param>
    void Update(TContext context, float deltaTime);

    /// <summary>
    /// 상태 종료 시 호출
    /// </summary>
    /// <param name="context">상태가 조작할 컨텍스트 객체</param>
    void Exit(TContext context);
}
