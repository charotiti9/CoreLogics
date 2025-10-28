using System;

/// <summary>
/// Update 주기에 실행되는 객체 인터페이스
/// Dispose()에서 반드시 UnregisterUpdatable()를 호출해야 함
/// </summary>
public interface IUpdatable : IDisposable
{
    /// <summary>
    /// 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    int UpdateOrder { get; }

    /// <summary>
    /// 매 프레임 호출되는 업데이트 메서드
    /// </summary>
    /// <param name="deltaTime">이전 프레임으로부터 경과된 시간</param>
    void OnUpdate(float deltaTime);
}
