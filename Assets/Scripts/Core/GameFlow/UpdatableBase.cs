using System;
using UnityEngine;
using Core.Utilities;

/// <summary>
/// IUpdatable 구현을 위한 베이스 클래스
/// 자동으로 등록/해제 추적 및 경고 제공
/// </summary>
public abstract class UpdatableBase : IUpdatable
{
    private bool isRegistered = false;
    private bool isDisposed = false;

    /// <summary>
    /// 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    public abstract int UpdateOrder { get; }

    /// <summary>
    /// GameFlowManager에 등록
    /// Initialize() 등에서 호출
    /// </summary>
    protected void Register()
    {
        if (!isRegistered)
        {
            GameFlowManager.Instance.RegisterUpdatable(this);
            isRegistered = true;
        }
    }

    /// <summary>
    /// GameFlowManager에서 등록 해제
    /// Dispose()에서 자동 호출됨
    /// </summary>
    protected void Unregister()
    {
        if (isRegistered)
        {
            GameFlowManager.Instance.UnregisterUpdatable(this);
            isRegistered = false;
        }
    }

    /// <summary>
    /// 매 프레임 호출되는 업데이트 메서드
    /// 파생 클래스에서 구현
    /// </summary>
    public abstract void OnUpdate(float deltaTime);

    /// <summary>
    /// 리소스 해제 - 반드시 호출해야 함
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
            return;

        Unregister();
        OnDispose();
        isDisposed = true;

        // 소멸자 호출 방지 (성능 최적화)
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 파생 클래스에서 추가 정리 로직 구현
    /// </summary>
    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// 소멸자 - Dispose()를 호출하지 않았을 때 경고
    /// </summary>
    ~UpdatableBase()
    {
        if (isRegistered && !isDisposed)
        {
            GameLogger.LogWarning($"[메모리 누수 경고] {GetType().Name}이(가) Dispose()되지 않고 소멸되었습니다. " +
                           $"GameFlowManager.UnregisterUpdatable()을 호출하지 않았습니다.");
        }
    }
}
