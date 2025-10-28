using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 게임 플로우를 관리하는 싱글톤 매니저
/// Update, FixedUpdate, LateUpdate를 중앙에서 관리하여 실행 순서를 명확히 제어
/// </summary>
public class GameFlowManager : MonoSingleton<GameFlowManager>
{
    // 업데이트 대상 리스트
    private List<IUpdatable> updatables;
    private List<IFixedUpdatable> fixedUpdatables;
    private List<ILateUpdatable> lateUpdatables;

    // 등록/해제 대기 리스트 (반복문 중 안전한 처리)
    private List<IUpdatable> pendingAddUpdatables;
    private List<IUpdatable> pendingRemoveUpdatables;
    private List<IFixedUpdatable> pendingAddFixedUpdatables;
    private List<IFixedUpdatable> pendingRemoveFixedUpdatables;
    private List<ILateUpdatable> pendingAddLateUpdatables;
    private List<ILateUpdatable> pendingRemoveLateUpdatables;

    // 반복문 실행 중 플래그
    private bool isUpdating;
    private bool isFixedUpdating;
    private bool isLateUpdating;

    // 일시정지 상태
    private bool _isPaused;

    /// <summary>
    /// 일시정지 상태 (true일 때 모든 Update 중단)
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set => _isPaused = value;
    }

    // 정렬 필요 플래그
    private bool needsSortUpdatables;
    private bool needsSortFixedUpdatables;
    private bool needsSortLateUpdatables;

    /// <summary>
    /// DontDestroyOnLoad 적용 (씬 전환 시에도 유지)
    /// </summary>
    protected override bool IsPersistent => true;

    /// <summary>
    /// 초기화
    /// </summary>
    protected override void Initialize()
    {
        // 리스트 초기화 (초기 capacity 설정으로 성능 최적화)
        updatables = new List<IUpdatable>(32);
        fixedUpdatables = new List<IFixedUpdatable>(32);
        lateUpdatables = new List<ILateUpdatable>(32);

        pendingAddUpdatables = new List<IUpdatable>(16);
        pendingRemoveUpdatables = new List<IUpdatable>(16);
        pendingAddFixedUpdatables = new List<IFixedUpdatable>(16);
        pendingRemoveFixedUpdatables = new List<IFixedUpdatable>(16);
        pendingAddLateUpdatables = new List<ILateUpdatable>(16);
        pendingRemoveLateUpdatables = new List<ILateUpdatable>(16);

        // 초기 상태
        _isPaused = false;
        isUpdating = false;
        isFixedUpdating = false;
        isLateUpdating = false;
    }

    #region IUpdatable 등록/해제

    /// <summary>
    /// IUpdatable 객체 등록
    /// </summary>
    public void RegisterUpdatable(IUpdatable updatable)
    {
        if (updatable == null)
            return;

        if (isUpdating)
        {
            // 반복문 실행 중이면 pending 리스트에 추가
            if (!pendingAddUpdatables.Contains(updatable))
            {
                pendingAddUpdatables.Add(updatable);
            }
        }
        else
        {
            // 중복 등록 방지
            if (!updatables.Contains(updatable))
            {
                updatables.Add(updatable);
                needsSortUpdatables = true;
            }
        }
    }

    /// <summary>
    /// IUpdatable 객체 등록 해제
    /// </summary>
    public void UnregisterUpdatable(IUpdatable updatable)
    {
        if (updatable == null)
            return;

        if (isUpdating)
        {
            // 반복문 실행 중이면 pending 리스트에 추가
            if (!pendingRemoveUpdatables.Contains(updatable))
            {
                pendingRemoveUpdatables.Add(updatable);
            }
        }
        else
        {
            updatables.Remove(updatable);
        }
    }

    #endregion

    #region IFixedUpdatable 등록/해제

    /// <summary>
    /// IFixedUpdatable 객체 등록
    /// </summary>
    public void RegisterFixedUpdatable(IFixedUpdatable fixedUpdatable)
    {
        if (fixedUpdatable == null)
            return;

        if (isFixedUpdating)
        {
            if (!pendingAddFixedUpdatables.Contains(fixedUpdatable))
            {
                pendingAddFixedUpdatables.Add(fixedUpdatable);
            }
        }
        else
        {
            if (!fixedUpdatables.Contains(fixedUpdatable))
            {
                fixedUpdatables.Add(fixedUpdatable);
                needsSortFixedUpdatables = true;
            }
        }
    }

    /// <summary>
    /// IFixedUpdatable 객체 등록 해제
    /// </summary>
    public void UnregisterFixedUpdatable(IFixedUpdatable fixedUpdatable)
    {
        if (fixedUpdatable == null)
            return;

        if (isFixedUpdating)
        {
            if (!pendingRemoveFixedUpdatables.Contains(fixedUpdatable))
            {
                pendingRemoveFixedUpdatables.Add(fixedUpdatable);
            }
        }
        else
        {
            fixedUpdatables.Remove(fixedUpdatable);
        }
    }

    #endregion

    #region ILateUpdatable 등록/해제

    /// <summary>
    /// ILateUpdatable 객체 등록
    /// </summary>
    public void RegisterLateUpdatable(ILateUpdatable lateUpdatable)
    {
        if (lateUpdatable == null)
            return;

        if (isLateUpdating)
        {
            if (!pendingAddLateUpdatables.Contains(lateUpdatable))
            {
                pendingAddLateUpdatables.Add(lateUpdatable);
            }
        }
        else
        {
            if (!lateUpdatables.Contains(lateUpdatable))
            {
                lateUpdatables.Add(lateUpdatable);
                needsSortLateUpdatables = true;
            }
        }
    }

    /// <summary>
    /// ILateUpdatable 객체 등록 해제
    /// </summary>
    public void UnregisterLateUpdatable(ILateUpdatable lateUpdatable)
    {
        if (lateUpdatable == null)
            return;

        if (isLateUpdating)
        {
            if (!pendingRemoveLateUpdatables.Contains(lateUpdatable))
            {
                pendingRemoveLateUpdatables.Add(lateUpdatable);
            }
        }
        else
        {
            lateUpdatables.Remove(lateUpdatable);
        }
    }

    #endregion

    #region Unity 생명주기

    private void Update()
    {
        // 일시정지 상태면 실행 중단
        if (_isPaused)
            return;

        isUpdating = true;

        // Pending 처리
        ProcessPendingUpdatables();

        // 정렬 필요 시 정렬
        if (needsSortUpdatables)
        {
            SortUpdatables();
            needsSortUpdatables = false;
        }

        // 등록된 객체들 업데이트
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < updatables.Count; i++)
        {
            updatables[i].OnUpdate(deltaTime);
        }

        isUpdating = false;
    }

    private void FixedUpdate()
    {
        // 일시정지 상태면 실행 중단
        if (_isPaused)
            return;

        isFixedUpdating = true;

        // Pending 처리
        ProcessPendingFixedUpdatables();

        // 정렬 필요 시 정렬
        if (needsSortFixedUpdatables)
        {
            SortFixedUpdatables();
            needsSortFixedUpdatables = false;
        }

        // 등록된 객체들 업데이트
        float fixedDeltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < fixedUpdatables.Count; i++)
        {
            fixedUpdatables[i].OnFixedUpdate(fixedDeltaTime);
        }

        isFixedUpdating = false;
    }

    private void LateUpdate()
    {
        // 일시정지 상태면 실행 중단
        if (_isPaused)
            return;

        isLateUpdating = true;

        // Pending 처리
        ProcessPendingLateUpdatables();

        // 정렬 필요 시 정렬
        if (needsSortLateUpdatables)
        {
            SortLateUpdatables();
            needsSortLateUpdatables = false;
        }

        // 등록된 객체들 업데이트
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < lateUpdatables.Count; i++)
        {
            lateUpdatables[i].OnLateUpdate(deltaTime);
        }

        isLateUpdating = false;
    }

    #endregion

    #region Pending 처리

    /// <summary>
    /// 대기 중인 IUpdatable 등록/해제 처리
    /// </summary>
    private void ProcessPendingUpdatables()
    {
        // 등록 대기 처리
        for (int i = 0; i < pendingAddUpdatables.Count; i++)
        {
            var updatable = pendingAddUpdatables[i];
            if (!updatables.Contains(updatable))
            {
                updatables.Add(updatable);
                needsSortUpdatables = true;
            }
        }
        pendingAddUpdatables.Clear();

        // 해제 대기 처리
        for (int i = 0; i < pendingRemoveUpdatables.Count; i++)
        {
            updatables.Remove(pendingRemoveUpdatables[i]);
        }
        pendingRemoveUpdatables.Clear();
    }

    /// <summary>
    /// 대기 중인 IFixedUpdatable 등록/해제 처리
    /// </summary>
    private void ProcessPendingFixedUpdatables()
    {
        // 등록 대기 처리
        for (int i = 0; i < pendingAddFixedUpdatables.Count; i++)
        {
            var fixedUpdatable = pendingAddFixedUpdatables[i];
            if (!fixedUpdatables.Contains(fixedUpdatable))
            {
                fixedUpdatables.Add(fixedUpdatable);
                needsSortFixedUpdatables = true;
            }
        }
        pendingAddFixedUpdatables.Clear();

        // 해제 대기 처리
        for (int i = 0; i < pendingRemoveFixedUpdatables.Count; i++)
        {
            fixedUpdatables.Remove(pendingRemoveFixedUpdatables[i]);
        }
        pendingRemoveFixedUpdatables.Clear();
    }

    /// <summary>
    /// 대기 중인 ILateUpdatable 등록/해제 처리
    /// </summary>
    private void ProcessPendingLateUpdatables()
    {
        // 등록 대기 처리
        for (int i = 0; i < pendingAddLateUpdatables.Count; i++)
        {
            var lateUpdatable = pendingAddLateUpdatables[i];
            if (!lateUpdatables.Contains(lateUpdatable))
            {
                lateUpdatables.Add(lateUpdatable);
                needsSortLateUpdatables = true;
            }
        }
        pendingAddLateUpdatables.Clear();

        // 해제 대기 처리
        for (int i = 0; i < pendingRemoveLateUpdatables.Count; i++)
        {
            lateUpdatables.Remove(pendingRemoveLateUpdatables[i]);
        }
        pendingRemoveLateUpdatables.Clear();
    }

    #endregion

    #region 정렬

    /// <summary>
    /// IUpdatable 리스트를 우선순위 기반으로 정렬
    /// </summary>
    private void SortUpdatables()
    {
        updatables.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
    }

    /// <summary>
    /// IFixedUpdatable 리스트를 우선순위 기반으로 정렬
    /// </summary>
    private void SortFixedUpdatables()
    {
        fixedUpdatables.Sort((a, b) => a.FixedUpdateOrder.CompareTo(b.FixedUpdateOrder));
    }

    /// <summary>
    /// ILateUpdatable 리스트를 우선순위 기반으로 정렬
    /// </summary>
    private void SortLateUpdatables()
    {
        lateUpdatables.Sort((a, b) => a.LateUpdateOrder.CompareTo(b.LateUpdateOrder));
    }

    #endregion
}
