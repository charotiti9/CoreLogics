using System;
using UnityEngine;

/// <summary>
/// Generic 상태 머신
/// IUpdatable을 구현하여 GameFlowManager와 자동 통합
/// 상태 전환 검증 및 이벤트 지원
/// </summary>
/// <typeparam name="TContext">상태가 조작할 컨텍스트 타입 (class 제약)</typeparam>
public class StateMachine<TContext> : IUpdatable where TContext : class
{
    private TContext context;
    private IState<TContext> currentState;
    private IState<TContext> previousState;
    private bool isDisposed;

    /// <summary>
    /// 현재 활성 상태
    /// </summary>
    public IState<TContext> CurrentState => currentState;

    /// <summary>
    /// 이전 상태 (상태 전환 후 참조 가능)
    /// </summary>
    public IState<TContext> PreviousState => previousState;

    /// <summary>
    /// 상태 머신이 조작하는 컨텍스트 객체
    /// </summary>
    public TContext Context => context;

    /// <summary>
    /// 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    public int UpdateOrder { get; set; } = 0;

    /// <summary>
    /// 상태 전환 검증 델리게이트
    /// null이 아닌 경우, ChangeState 호출 시 검증 수행
    /// false 반환 시 상태 전환이 취소됨
    /// </summary>
    /// <param name="from">현재 상태 (null 가능)</param>
    /// <param name="to">전환하려는 상태</param>
    /// <returns>전환 허용 여부</returns>
    public delegate bool StateTransitionValidator(IState<TContext> from, IState<TContext> to);

    /// <summary>
    /// 상태 전환 검증자 설정
    /// </summary>
    public StateTransitionValidator TransitionValidator { get; set; }

    /// <summary>
    /// 상태 전환 이벤트
    /// 상태 전환 완료 후 발생 (Enter 호출 후)
    /// </summary>
    /// <param name="previousState">이전 상태 (null 가능)</param>
    /// <param name="currentState">현재 상태</param>
    public event Action<IState<TContext>, IState<TContext>> OnStateChanged;

    /// <summary>
    /// StateMachine 생성자 (PleaseOneMoreBlock 스타일)
    /// 생성과 동시에 초기화 및 GameFlowManager에 자동 등록
    /// </summary>
    /// <param name="context">상태가 조작할 컨텍스트 객체</param>
    public StateMachine(TContext context)
    {
        if (context == null)
        {
            Debug.LogError("[StateMachine] Context가 null입니다. 초기화를 중단합니다.");
            return;
        }

        this.context = context;

        // GameFlowManager에 등록
        GameFlowManager.Instance.RegisterUpdatable(this);
    }

    /// <summary>
    /// 상태 전환 (PleaseOneMoreBlock의 Next 메서드 스타일)
    /// TransitionValidator가 설정된 경우 검증 수행
    /// </summary>
    /// <param name="newState">전환할 새 상태</param>
    /// <returns>전환 성공 여부</returns>
    public bool ChangeState(IState<TContext> newState)
    {
        if (newState == null)
        {
            Debug.LogError("[StateMachine] 전환하려는 상태가 null입니다.");
            return false;
        }

        // 상태 전환 검증
        if (TransitionValidator != null && !TransitionValidator(currentState, newState))
        {
            Debug.LogWarning($"[StateMachine] 상태 전환이 거부되었습니다. " +
                           $"From: {currentState?.GetType().Name ?? "null"}, " +
                           $"To: {newState.GetType().Name}");
            return false;
        }

        // 현재 상태 종료
        currentState?.Exit(context);

        // 상태 전환
        previousState = currentState;
        currentState = newState;

        // 확장 포인트 (PleaseOneMoreBlock의 OnNext 패턴)
        OnNext();

        // 새 상태 진입
        currentState.Enter(context);

        // 상태 전환 이벤트 발생
        OnStateChanged?.Invoke(previousState, currentState);

        return true;
    }

    /// <summary>
    /// 상태 전환 시 호출되는 확장 포인트 (PleaseOneMoreBlock 스타일)
    /// 파생 클래스에서 오버라이드하여 상태 전환 시 추가 로직 삽입 가능
    /// </summary>
    protected virtual void OnNext()
    {
    }

    /// <summary>
    /// 이전 상태로 되돌림
    /// PreviousState가 null인 경우 false 반환
    /// </summary>
    /// <returns>되돌리기 성공 여부</returns>
    public bool RevertToPreviousState()
    {
        if (previousState == null)
        {
            Debug.LogWarning("[StateMachine] 이전 상태가 없습니다. 되돌릴 수 없습니다.");
            return false;
        }

        return ChangeState(previousState);
    }

    /// <summary>
    /// 매 프레임 호출 (IUpdatable 구현)
    /// GameFlowManager가 자동 호출
    /// </summary>
    public void OnUpdate(float deltaTime)
    {
        if (isDisposed)
            return;

        currentState?.Update(context, deltaTime);
    }

    /// <summary>
    /// 리소스 해제 (IDisposable 구현)
    /// 현재 상태 Exit 호출 및 GameFlowManager에서 등록 해제
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
            return;

        // 현재 상태 종료
        currentState?.Exit(context);

        // GameFlowManager에서 등록 해제
        GameFlowManager.Instance.UnregisterUpdatable(this);

        // 정리
        currentState = null;
        previousState = null;
        context = null;
        TransitionValidator = null;
        OnStateChanged = null;

        isDisposed = true;

        // GC 최적화
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 소멸자 - Dispose()를 호출하지 않았을 때 경고
    /// </summary>
    ~StateMachine()
    {
        if (!isDisposed)
        {
            Debug.LogWarning($"[메모리 누수 경고] StateMachine<{typeof(TContext).Name}>이(가) Dispose()되지 않고 소멸되었습니다.");
        }
    }
}
