using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

namespace Common.UI
{
    /// <summary>
    /// UI 애니메이션 베이스 클래스 (DOTween 기반)
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
    /// 페이드 인/아웃 애니메이션 (DOTween 기반)
    /// </summary>
    public class UIFadeAnimation : UIAnimation
    {
        private readonly float fromAlpha;
        private readonly float toAlpha;
        private readonly float duration;
        private readonly Ease ease;

        /// <summary>
        /// 페이드 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="fromAlpha">시작 알파값 (0~1)</param>
        /// <param name="toAlpha">종료 알파값 (0~1)</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        /// <param name="ease">Easing 타입</param>
        public UIFadeAnimation(float fromAlpha, float toAlpha, float duration, Ease ease = Ease.Linear)
        {
            this.fromAlpha = fromAlpha;
            this.toAlpha = toAlpha;
            this.duration = duration;
            this.ease = ease;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            // 시작 알파값 설정
            canvasGroup.alpha = fromAlpha;

            // DOTween으로 페이드 애니메이션
            await canvasGroup.DOFade(toAlpha, duration)
                .SetEase(ease)
                .ToUniTask(cancellationToken: ct);
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
    /// 슬라이드 애니메이션 (DOTween 기반)
    /// </summary>
    public class UISlideAnimation : UIAnimation
    {
        private readonly SlideDirection direction;
        private readonly float distance;
        private readonly float duration;
        private readonly bool isShow;
        private readonly Ease ease;

        /// <summary>
        /// 슬라이드 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="direction">슬라이드 방향</param>
        /// <param name="distance">이동 거리 (픽셀)</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        /// <param name="isShow">표시 애니메이션 여부 (true: 표시, false: 숨김)</param>
        /// <param name="ease">Easing 타입</param>
        public UISlideAnimation(SlideDirection direction, float distance, float duration, bool isShow, Ease ease = Ease.OutQuad)
        {
            this.direction = direction;
            this.distance = distance;
            this.duration = duration;
            this.isShow = isShow;
            this.ease = ease;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            Vector2 startPos = target.anchoredPosition;
            Vector2 offset = GetOffsetByDirection(direction, distance);

            Vector2 fromPos = isShow ? startPos + offset : startPos;
            Vector2 toPos = isShow ? startPos : startPos + offset;

            // 시작 위치 설정
            target.anchoredPosition = fromPos;

            // DOTween으로 슬라이드 애니메이션
            await target.DOAnchorPos(toPos, duration)
                .SetEase(ease)
                .ToUniTask(cancellationToken: ct);
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
    /// 스케일 애니메이션 (DOTween 기반)
    /// </summary>
    public class UIScaleAnimation : UIAnimation
    {
        private readonly Vector3 fromScale;
        private readonly Vector3 toScale;
        private readonly float duration;
        private readonly Ease ease;

        /// <summary>
        /// 스케일 애니메이션을 생성합니다.
        /// </summary>
        /// <param name="fromScale">시작 스케일</param>
        /// <param name="toScale">종료 스케일</param>
        /// <param name="duration">애니메이션 지속 시간 (초)</param>
        /// <param name="ease">Easing 타입</param>
        public UIScaleAnimation(Vector3 fromScale, Vector3 toScale, float duration, Ease ease = Ease.OutBack)
        {
            this.fromScale = fromScale;
            this.toScale = toScale;
            this.duration = duration;
            this.ease = ease;
        }

        public override async UniTask PlayAsync(RectTransform target, CancellationToken ct)
        {
            // 시작 스케일 설정
            target.localScale = fromScale;

            // DOTween으로 스케일 애니메이션
            await target.DOScale(toScale, duration)
                .SetEase(ease)
                .ToUniTask(cancellationToken: ct);
        }
    }

    /// <summary>
    /// 여러 애니메이션을 순차적으로 재생 (DOTween Sequence 기반)
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
            // 각 애니메이션을 순차적으로 실행
            foreach (var animation in animations)
            {
                ct.ThrowIfCancellationRequested();
                await animation.PlayAsync(target, ct);
            }
        }
    }
}
