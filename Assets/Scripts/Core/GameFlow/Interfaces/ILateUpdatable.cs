using System;

/// <summary>
/// LateUpdate 주기에 실행되는 객체 인터페이스
/// 카메라 추적 등 모든 Update 이후 실행되어야 하는 로직에 사용
/// Dispose()에서 반드시 UnregisterLateUpdatable()를 호출해야 함
/// </summary>
public interface ILateUpdatable : IDisposable
{
    /// <summary>
    /// 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    int LateUpdateOrder { get; }

    /// <summary>
    /// 모든 Update 이후 호출되는 업데이트 메서드
    /// </summary>
    /// <param name="deltaTime">이전 프레임으로부터 경과된 시간</param>
    void OnLateUpdate(float deltaTime);
}
