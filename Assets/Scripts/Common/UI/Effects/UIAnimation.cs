using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// UI 애니메이션 베이스 클래스
    /// </summary>
    public abstract class UIAnimation
    {
        /// <summary>
        /// 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="target">대상 RectTransform</param>
        /// <param name="ct">CancellationToken</param>
        public abstract UniTask PlayAsync(RectTransform target, CancellationToken ct);
    }

    /// <summary>
    /// 페이드 인/아웃 애니메이션
    /// </summary>
    public class UIFadeAnimation : UIAnimation
    {
        private readonly float fromAlpha;
        private readonly float toAlpha;
        private readonly float duration;

        /// <summary>
        /// 페이드 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="fromAlpha">시작 알파값 (0~1)</param>
        /// <param name="toAlpha">종료 알파값 (0~1)</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        public UIFadeAnimation(float fromAlpha, float toAlpha, float duration)
        {
            this.fromAlpha = fromAlpha;
            this.toAlpha = toAlpha;
            this.duration = duration;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = toAlpha;
        }
    }

    /// <summary>
    /// 슬라이드 애니메이션 방향
    /// </summary>
    public enum SlideDirection
    {
        Up,     // 위에서 아래로
        Down,   // 아래에서 위로
        Left,   // 왼쪽에서 오른쪽으로
        Right   // 오른쪽에서 왼쪽으로
    }

    /// <summary>
    /// 슬라이드 애니메이션
    /// </summary>
    public class UISlideAnimation : UIAnimation
    {
        private readonly SlideDirection direction;
        private readonly float distance;
        private readonly float duration;
        private readonly bool isShow;

        /// <summary>
        /// 슬라이드 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="direction">슬라이드 방향</param>
        /// <param name="distance">이동 거리 (픽셀)</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        /// <param name="isShow">표시 애니메이션 여부 (true: 표시, false: 숨김)</param>
        public UISlideAnimation(SlideDirection direction, float distance, float duration, bool isShow)
        {
            this.direction = direction;
            this.distance = distance;
            this.duration = duration;
            this.isShow = isShow;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            Vector2 startPos = target.anchoredPosition;
            Vector2 offset = GetOffsetByDirection(direction, distance);

            Vector2 fromPos = isShow ? startPos + offset : startPos;
            Vector2 toPos = isShow ? startPos : startPos + offset;

            // 시작 위치 설정
            target.anchoredPosition = fromPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.anchoredPosition = Vector2.Lerp(fromPos, toPos, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            target.anchoredPosition = toPos;
        }

        private Vector2 GetOffsetByDirection(SlideDirection dir, float dist)
        {
            switch (dir)
            {
                case SlideDirection.Up:
                    return new Vector2(0, dist);
                case SlideDirection.Down:
                    return new Vector2(0, -dist);
                case SlideDirection.Left:
                    return new Vector2(-dist, 0);
                case SlideDirection.Right:
                    return new Vector2(dist, 0);
                default:
                    return Vector2.zero;
            }
        }
    }

    /// <summary>
    /// 스케일 애니메이션
    /// </summary>
    public class UIScaleAnimation : UIAnimation
    {
        private readonly Vector3 fromScale;
        private readonly Vector3 toScale;
        private readonly float duration;

        /// <summary>
        /// 스케일 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="fromScale">시작 스케일</param>
        /// <param name="toScale">종료 스케일</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        public UIScaleAnimation(Vector3 fromScale, Vector3 toScale, float duration)
        {
            this.fromScale = fromScale;
            this.toScale = toScale;
            this.duration = duration;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            target.localScale = fromScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.Lerp(fromScale, toScale, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            target.localScale = toScale;
        }
    }

    /// <summary>
    /// 여러 애니메이션을 순차적으로 재생
    /// </summary>
    public class UISequenceAnimation : UIAnimation
    {
        private readonly UIAnimation[] animations;

        /// <summary>
        /// 순차 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="animations">재생할 애니메이션 배열</param>
        public UISequenceAnimation(params UIAnimation[] animations)
        {
            this.animations = animations;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            foreach (var animation in animations)
            {
                ct.ThrowIfCancellationRequested();
                await animation.PlayAsync(target, ct);
            }
        }
    }
}
