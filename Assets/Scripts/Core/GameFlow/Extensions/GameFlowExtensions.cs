using Core.Utilities;
using UnityEngine;

/// <summary>
/// GameFlow 관련 인터페이스 확장 메서드
/// IUpdatable, IFixedUpdatable, ILateUpdatable의 등록/해제를 간소화합니다.
/// </summary>
public static class GameFlowExtensions
{
    // ========== IUpdatable ==========

    /// <summary>
    /// GameFlowManager에 IUpdatable 등록
    /// </summary>
    public static void RegisterToGameFlow(this IUpdatable updatable)
    {
        if (updatable == null)
        {
            GameLogger.LogError("[GameFlow] null IUpdatable을 등록할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.RegisterUpdatable(updatable);
        }
        else
        {
            GameLogger.LogWarning($"[GameFlow] GameFlowManager가 없습니다. {updatable.GetType().Name} 등록 실패");
        }
    }

    /// <summary>
    /// GameFlowManager에서 IUpdatable 등록 해제
    /// </summary>
    public static void UnregisterFromGameFlow(this IUpdatable updatable)
    {
        if (updatable == null)
        {
            GameLogger.LogError("[GameFlow] null IUpdatable을 등록 해제할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.UnregisterUpdatable(updatable);
        }
    }

    // ========== IFixedUpdatable ==========

    /// <summary>
    /// GameFlowManager에 IFixedUpdatable 등록
    /// </summary>
    public static void RegisterToGameFlow(this IFixedUpdatable fixedUpdatable)
    {
        if (fixedUpdatable == null)
        {
            GameLogger.LogError("[GameFlow] null IFixedUpdatable을 등록할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.RegisterFixedUpdatable(fixedUpdatable);
        }
        else
        {
            GameLogger.LogWarning($"[GameFlow] GameFlowManager가 없습니다. {fixedUpdatable.GetType().Name} 등록 실패");
        }
    }

    /// <summary>
    /// GameFlowManager에서 IFixedUpdatable 등록 해제
    /// </summary>
    public static void UnregisterFromGameFlow(this IFixedUpdatable fixedUpdatable)
    {
        if (fixedUpdatable == null)
        {
            GameLogger.LogError("[GameFlow] null IFixedUpdatable을 등록 해제할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.UnregisterFixedUpdatable(fixedUpdatable);
        }
    }

    // ========== ILateUpdatable ==========

    /// <summary>
    /// GameFlowManager에 ILateUpdatable 등록
    /// </summary>
    public static void RegisterToGameFlow(this ILateUpdatable lateUpdatable)
    {
        if (lateUpdatable == null)
        {
            GameLogger.LogError("[GameFlow] null ILateUpdatable을 등록할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.RegisterLateUpdatable(lateUpdatable);
        }
        else
        {
            GameLogger.LogWarning($"[GameFlow] GameFlowManager가 없습니다. {lateUpdatable.GetType().Name} 등록 실패");
        }
    }

    /// <summary>
    /// GameFlowManager에서 ILateUpdatable 등록 해제
    /// </summary>
    public static void UnregisterFromGameFlow(this ILateUpdatable lateUpdatable)
    {
        if (lateUpdatable == null)
        {
            GameLogger.LogError("[GameFlow] null ILateUpdatable을 등록 해제할 수 없습니다.");
            return;
        }

        if (GameFlowManager.IsAlive())
        {
            GameFlowManager.Instance.UnregisterLateUpdatable(lateUpdatable);
        }
    }
}
