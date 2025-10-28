using System;

/// <summary>
/// FixedUpdate 주기에 실행되는 객체 인터페이스
/// 물리 연산 등 고정 시간 간격이 필요한 로직에 사용
/// Dispose()에서 반드시 UnregisterFixedUpdatable()를 호출해야 함
/// </summary>
public interface IFixedUpdatable : IDisposable
{
    /// <summary>
    /// 실행 우선순위 (낮을수록 먼저 실행)
    /// </summary>
    int FixedUpdateOrder { get; }

    /// <summary>
    /// 고정 시간 간격으로 호출되는 업데이트 메서드
    /// </summary>
    /// <param name="fixedDeltaTime">고정 시간 간격</param>
    void OnFixedUpdate(float fixedDeltaTime);
}
