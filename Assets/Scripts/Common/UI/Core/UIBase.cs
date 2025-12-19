using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Core.Utilities;

namespace Common.UI
{
    /// <summary>
    /// 모든 UI의 베이스 클래스
    /// UI의 생명주기와 공통 기능을 관리합니다.
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        private RectTransform rectTransform;

        // 생명주기 관리용 CancellationTokenSource
        private CancellationTokenSource lifecycleCts;

        // 애니메이션 중첩 방지용 CancellationTokenSource
        private CancellationTokenSource animationCts;

        /// <summary>
        /// UI가 속한 레이어
        /// </summary>
        public abstract UILayer Layer { get; }

        /// <summary>
        /// Dim 사용 여부
        /// true: Show 시 자동으로 Dim 표시, Hide 시 자동으로 Dim 숨김
        /// false: Dim 미사용 (기본값)
        /// </summary>
        public virtual bool UseDim => false;

        /// <summary>
        /// 씬 변경 시 자동으로 제거할지 여부
        /// true: 씬 변경 시 자동 제거 (기본값)
        /// false: 씬 전환 후에도 유지 (예: HUD, 로딩 UI)
        /// </summary>
        public virtual bool DestroyOnSceneChange => true;

        /// <summary>
        /// Spawn 여부 (인스턴스가 메모리에 생성되어 있는지)
        /// </summary>
        public bool IsSpawned { get; private set; }

        /// <summary>
        /// 현재 표시 중인지 여부
        /// </summary>
        public bool IsShowing { get; private set; }

        /// <summary>
        /// RectTransform 캐싱
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }

        /// <summary>
        /// 표시 애니메이션
        /// null이면 즉시 표시됩니다.
        /// </summary>
        public virtual UIAnimation ShowAnimation => null;

        /// <summary>
        /// 숨김 애니메이션
        /// null이면 즉시 숨겨집니다.
        /// </summary>
        public virtual UIAnimation HideAnimation => null;

        /// <summary>
        /// Spawn 시 1회 호출됩니다. (Addressable 로드 완료 후)
        /// UI의 초기 설정을 여기서 수행합니다.
        /// </summary>
        public virtual void OnSpawn()
        {
            // 파생 클래스에서 오버라이드
        }

        /// <summary>
        /// Destroy 전 호출됩니다.
        /// 리소스 정리 작업을 여기서 수행합니다.
        /// </summary>
        public virtual void OnBeforeDestroy()
        {
            // 파생 클래스에서 오버라이드
        }

        /// <summary>
        /// UI가 표시될 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public virtual async UniTask OnShowAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// UI가 숨겨질 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public virtual async UniTask OnHideAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// 해상도가 변경될 때 호출됩니다.
        /// </summary>
        /// <param name="newResolution">새로운 해상도</param>
        public virtual void OnResolutionChanged(Vector2Int newResolution)
        {
            // 각 UI에서 필요 시 오버라이드하여 처리
        }

        /// <summary>
        /// 내부적으로 UI를 표시합니다.
        /// UIManager에서 호출합니다.
        ///
        /// 생명주기:
        /// 1. GameObject.SetActive(true)
        /// 2. OnShowAsync
        /// 3. ShowAnimation
        /// </summary>
        internal async UniTask ShowInternalAsync(object data, CancellationToken ct)
        {
            // 이전 애니메이션 취소 (중첩 실행 방지)
            animationCts?.Cancel();
            animationCts?.Dispose();
            animationCts = new CancellationTokenSource();

            // 생명주기 CTS, 외부 CT, 애니메이션 CTS를 결합
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                lifecycleCts.Token,
                animationCts.Token))
            {
                try
                {
                    gameObject.SetActive(true);
                    IsShowing = true;

                    // 커스텀 Show 로직 (애니메이션 전에 UI 데이터 설정)
                    await OnShowAsync(linkedCts.Token);

                    // 표시 애니메이션 재생 (UI 준비 완료 후)
                    if (ShowAnimation != null)
                    {
                        await ShowAnimation.PlayAsync(RectTransform, linkedCts.Token);
                    }
                }
                catch(OperationCanceledException)
                {
                    // 취소된 경우 로그 출력 (정상 동작)
                    GameLogger.Log($"[UIBase] {GetType().Name} Show 작업이 취소되었습니다.");
                    throw;
                }
            }
        }

        /// <summary>
        /// 내부적으로 UI를 숨깁니다.
        /// UIManager에서 호출합니다.
        ///
        /// 생명주기:
        /// 1. OnHideAsync (정리 로직)
        /// 2. HideAnimation (즉시 숨김이 아닌 경우)
        /// 3. GameObject.SetActive(false)
        /// </summary>
        internal async UniTask HideInternalAsync(bool immediate, CancellationToken ct)
        {
            // 이전 애니메이션 취소 (중첩 실행 방지)
            animationCts?.Cancel();
            animationCts?.Dispose();
            animationCts = new CancellationTokenSource();

            // 생명주기 CTS, 외부 CT, 애니메이션 CTS를 결합
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                lifecycleCts.Token,
                animationCts.Token))
            {
                try
                {
                    IsShowing = false;

                    // 커스텀 Hide 로직 (정리 작업)
                    await OnHideAsync(linkedCts.Token);

                    // 숨김 애니메이션 재생 (즉시 숨김이 아닌 경우)
                    if (!immediate && HideAnimation != null)
                    {
                        await HideAnimation.PlayAsync(RectTransform, linkedCts.Token);
                    }

                    gameObject.SetActive(false);
                }
                catch (OperationCanceledException)
                {
                    // 취소된 경우 로그 출력 (정상 동작)
                    GameLogger.Log($"[UIBase] {GetType().Name} Hide 작업이 취소되었습니다.");
                    throw;
                }
            }
        }

        /// <summary>
        /// Spawn 시 내부적으로 호출됩니다. (UIManager 전용)
        /// </summary>
        internal void SpawnInternal()
        {
            IsSpawned = true;

            // 생명주기 CancellationTokenSource 생성
            lifecycleCts = new CancellationTokenSource();
        }

        /// <summary>
        /// Destroy 시 내부적으로 호출됩니다. (UIManager 전용)
        /// </summary>
        internal void DestroyInternal()
        {
            IsSpawned = false;

            // 진행 중인 모든 비동기 작업 취소
            lifecycleCts?.Cancel();
            lifecycleCts?.Dispose();

            animationCts?.Cancel();
            animationCts?.Dispose();
        }

        /// <summary>
        /// Static Helper: UI를 표시합니다.
        /// 각 UI 클래스에서 이 메서드를 사용하여 편리한 static Show 메서드를 만들 수 있습니다.
        /// </summary>
        protected static async UniTask<T> ShowUI<T>(
            object data = null,
            CancellationToken ct = default
        ) where T : UIBase
        {
            return await UIManager.Instance.ShowAsync<T>(data, ct);
        }

        /// <summary>
        /// Static Helper: UI를 숨깁니다.
        /// </summary>
        protected static void HideUI<T>(bool immediate = false) where T : UIBase
        {
            UIManager.Instance.Hide<T>(immediate);
        }

        private void OnDestroy()
        {
            // 진행 중인 모든 비동기 작업 취소
            lifecycleCts?.Cancel();
            lifecycleCts?.Dispose();

            animationCts?.Cancel();
            animationCts?.Dispose();
        }
    }

    /// <summary>
    /// 타입 안전한 데이터를 지원하는 UIBase 제네릭 버전
    /// </summary>
    /// <typeparam name="TData">UI 데이터 타입 (class만 허용)</typeparam>
    public abstract class UIBase<TData> : UIBase where TData : class
    {
        /// <summary>
        /// Static Helper: UI를 표시합니다. (제네릭 버전)
        /// </summary>
        protected static async UniTask<TUI> ShowUI<TUI, TUIData>(
            TUIData data = null,
            CancellationToken ct = default
        ) where TUI : UIBase<TUIData>
          where TUIData : class
        {
            return await UIManager.Instance.ShowAsync<TUI, TUIData>(data, ct);
        }

        /// <summary>
        /// Static Helper: UI를 숨깁니다. (제네릭 버전)
        /// </summary>
        protected static void HideUI<TUI, TUIData>(bool immediate = false)
            where TUI : UIBase<TUIData>
            where TUIData : class
        {
            UIManager.Instance.Hide<TUI, TUIData>(immediate);
        }
    }
}

