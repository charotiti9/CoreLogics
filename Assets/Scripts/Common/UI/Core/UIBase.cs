using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// 모든 UI의 베이스 클래스
    /// UI의 생명주기와 공통 기능을 관리합니다.
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        private RectTransform rectTransform;
        private bool isInitialized;

        /// <summary>
        /// UI가 속한 레이어
        /// </summary>
        public abstract UILayer Layer { get; }

        /// <summary>
        /// 씬 변경 시 자동으로 제거할지 여부
        /// true: 씬 변경 시 자동 제거 (기본값)
        /// false: 씬 전환 후에도 유지 (예: HUD, 로딩 UI)
        /// </summary>
        public virtual bool DestroyOnSceneChange => true;

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
        /// 부모 Canvas
        /// </summary>
        public Canvas ParentCanvas { get; set; }

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
        /// 최초 생성 시 1회 호출됩니다.
        /// </summary>
        /// <param name="data">초기화 데이터</param>
        public virtual void OnInitialize(object data)
        {
            isInitialized = true;
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
        /// </summary>
        internal async UniTask ShowInternalAsync(object data, CancellationToken ct)
        {
            // 초기화되지 않았으면 초기화
            if (!isInitialized)
            {
                OnInitialize(data);
            }

            gameObject.SetActive(true);
            IsShowing = true;

            // 표시 애니메이션 재생
            if (ShowAnimation != null)
            {
                await ShowAnimation.PlayAsync(RectTransform, ct);
            }

            // 커스텀 Show 로직
            await OnShowAsync(ct);
        }

        /// <summary>
        /// 내부적으로 UI를 숨깁니다.
        /// UIManager에서 호출합니다.
        /// </summary>
        internal async UniTask HideInternalAsync(bool immediate, CancellationToken ct)
        {
            IsShowing = false;

            // 커스텀 Hide 로직
            await OnHideAsync(ct);

            // 숨김 애니메이션 재생 (즉시 숨김이 아닌 경우)
            if (!immediate && HideAnimation != null)
            {
                await HideAnimation.PlayAsync(RectTransform, ct);
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            isInitialized = false;
        }
    }
}
