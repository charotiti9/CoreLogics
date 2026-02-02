using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Common.UI;

namespace Common.SceneLoader
{
    /// <summary>
    /// 씬 전환 시 페이드 효과를 제공하는 UI
    /// 화면 전체를 덮는 이미지로 페이드 인/아웃 효과를 제공합니다.
    /// </summary>
    [UIAttribute(
        address: "Common/SceneFadeUI",
        layer: UILayer.Transition,
        useDim: false,
        destroyOnSceneChange: false)]
    public class SceneFadeUI : UIBase
    {
        [SerializeField]
        private Image fadeImage;

        [SerializeField]
        private CanvasGroup canvasGroup;

        private CancellationTokenSource fadeCts;

        /// <summary>
        /// 현재 페이드 색상
        /// </summary>
        public Color CurrentColor => fadeImage != null ? fadeImage.color : Color.black;

        /// <summary>
        /// UI가 생성될 때 호출됩니다.
        /// </summary>
        public override void OnSpawn()
        {
            // 컴포넌트 자동 할당 (Inspector에서 설정하지 않은 경우)
            if (fadeImage == null)
            {
                fadeImage = GetComponentInChildren<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponentInChildren<CanvasGroup>();
                if (canvasGroup == null && fadeImage != null)
                {
                    canvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 초기 상태: 투명
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// UI가 표시될 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public override async UniTask OnShowAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// UI가 숨겨질 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public override async UniTask OnHideAsync(CancellationToken ct)
        {
            CancelCurrentFade();
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// UI가 제거되기 전에 호출됩니다.
        /// </summary>
        public override void OnBeforeDestroy()
        {
            CancelCurrentFade();
        }

        /// <summary>
        /// 페이드 아웃 (화면을 지정 색상으로 덮음)
        /// Show 후에 호출해야 합니다.
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="duration">페이드 지속 시간</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask FadeOutAsync(Color color, float duration, CancellationToken ct)
        {
            CancelCurrentFade();

            fadeCts = new CancellationTokenSource();
            var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, fadeCts.Token).Token;

            try
            {
                fadeImage.color = new Color(color.r, color.g, color.b, 1f);
                await FadeAsync(0f, 1f, duration, linkedCt);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
        }

        /// <summary>
        /// 페이드 인 (페이드 색상을 제거하여 화면 표시)
        /// </summary>
        /// <param name="duration">페이드 지속 시간</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask FadeInAsync(float duration, CancellationToken ct)
        {
            Debug.Log($"[SceneFadeUI] FadeInAsync 시작 - canvasGroup: {canvasGroup != null}, gameObject: {gameObject != null}");

            CancelCurrentFade();

            fadeCts = new CancellationTokenSource();
            var linkedCt = CancellationTokenSource.CreateLinkedTokenSource(ct, fadeCts.Token).Token;

            try
            {
                Debug.Log($"[SceneFadeUI] FadeAsync 호출 전");
                await FadeAsync(1f, 0f, duration, linkedCt);
                Debug.Log($"[SceneFadeUI] FadeAsync 완료");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log($"[SceneFadeUI] FadeInAsync 취소됨");
                throw;
            }
        }

        /// <summary>
        /// 페이드 알파 값을 즉시 설정합니다.
        /// </summary>
        /// <param name="alpha">알파 값 (0~1)</param>
        public void SetAlphaImmediate(float alpha)
        {
            CancelCurrentFade();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        /// <summary>
        /// 페이드 색상과 알파를 즉시 설정합니다.
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="alpha">알파 값 (0~1)</param>
        public void SetColorImmediate(Color color, float alpha)
        {
            CancelCurrentFade();

            if (fadeImage != null)
            {
                fadeImage.color = new Color(color.r, color.g, color.b, 1f);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        /// <summary>
        /// 페이드 애니메이션 수행
        /// </summary>
        private async UniTask FadeAsync(float from, float to, float duration, CancellationToken ct)
        {
            Debug.Log($"[SceneFadeUI] FadeAsync 시작 - from: {from}, to: {to}, duration: {duration}");

            if (canvasGroup == null)
            {
                Debug.LogError($"[SceneFadeUI] canvasGroup이 null입니다.");
                return;
            }

            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                Debug.Log($"[SceneFadeUI] duration이 0이라 즉시 완료");
                return;
            }

            float elapsed = 0f;
            canvasGroup.alpha = from;
            int frameCount = 0;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                frameCount++;

                if (frameCount == 1)
                {
                    Debug.Log($"[SceneFadeUI] 첫 프레임 시작");
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (frameCount == 1)
                {
                    Debug.Log($"[SceneFadeUI] 첫 프레임 완료");
                }
            }

            canvasGroup.alpha = to;
            Debug.Log($"[SceneFadeUI] FadeAsync 완료 - 총 {frameCount} 프레임");
        }

        /// <summary>
        /// 현재 진행 중인 페이드 작업을 취소합니다.
        /// </summary>
        private void CancelCurrentFade()
        {
            if (fadeCts != null)
            {
                fadeCts.Cancel();
                fadeCts.Dispose();
                fadeCts = null;
            }
        }
    }
}
