using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Common.SceneLoader
{
    /// <summary>
    /// 씬 전환 옵션 설정
    /// 빌더 패턴을 지원하여 다양한 전환 방식을 쉽게 구성할 수 있습니다.
    /// </summary>
    public class SceneTransitionOptions
    {
        /// <summary>
        /// 씬 로드 완료 후 전환 여부
        /// true: 씬을 백그라운드에서 완전히 로드한 후 전환 (현재 씬 유지)
        /// false: 즉시 전환 시작 (기본값)
        /// </summary>
        public bool WaitForLoadComplete { get; set; } = false;

        /// <summary>
        /// 로딩 UI 표시 여부
        /// </summary>
        public bool ShowLoadingUI { get; set; } = false;

        /// <summary>
        /// 페이드 효과 사용 여부
        /// </summary>
        public bool UseFade { get; set; } = false;

        /// <summary>
        /// 페이드 색상
        /// </summary>
        public Color FadeColor { get; set; } = Color.black;

        /// <summary>
        /// 페이드 지속 시간 (초)
        /// </summary>
        public float FadeDuration { get; set; } = 0.5f;

        /// <summary>
        /// 씬 로드 진행률 콜백 (0.0 ~ 1.0)
        /// </summary>
        public Action<float> OnProgress { get; set; } = null;

        /// <summary>
        /// 씬 활성화 직후, 화면을 다시 보여주기 전에 실행할 준비 작업
        /// </summary>
        public Func<CancellationToken, UniTask> OnSceneReady { get; set; } = null;

        /// <summary>
        /// 기본 전환 옵션 (효과 없이 즉시 전환)
        /// </summary>
        public static SceneTransitionOptions Default => new SceneTransitionOptions();

        /// <summary>
        /// 로딩 UI만 표시하는 옵션
        /// </summary>
        public static SceneTransitionOptions WithLoading()
        {
            return new SceneTransitionOptions
            {
                ShowLoadingUI = true
            };
        }

        /// <summary>
        /// 페이드 효과만 적용하는 옵션
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="duration">페이드 지속 시간</param>
        public static SceneTransitionOptions WithFade(Color color, float duration = 0.5f)
        {
            return new SceneTransitionOptions
            {
                UseFade = true,
                FadeColor = color,
                FadeDuration = duration
            };
        }

        /// <summary>
        /// 로딩 UI와 페이드 효과를 모두 적용하는 옵션
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="duration">페이드 지속 시간</param>
        public static SceneTransitionOptions WithLoadingAndFade(Color color, float duration = 0.5f)
        {
            return new SceneTransitionOptions
            {
                ShowLoadingUI = true,
                UseFade = true,
                FadeColor = color,
                FadeDuration = duration
            };
        }

        /// <summary>
        /// 씬 로드 완료 후 전환 옵션 (효과 없음)
        /// 현재 씬을 유지하면서 새 씬을 백그라운드에서 로드합니다.
        /// </summary>
        public static SceneTransitionOptions Preloaded()
        {
            return new SceneTransitionOptions
            {
                WaitForLoadComplete = true
            };
        }

        /// <summary>
        /// 씬 로드 완료 후 전환 + 로딩 UI 옵션
        /// </summary>
        public static SceneTransitionOptions PreloadedWithLoading()
        {
            return new SceneTransitionOptions
            {
                WaitForLoadComplete = true,
                ShowLoadingUI = true
            };
        }

        /// <summary>
        /// 씬 로드 완료 후 전환 + 페이드 효과 옵션
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="duration">페이드 지속 시간</param>
        public static SceneTransitionOptions PreloadedWithFade(Color color, float duration = 0.5f)
        {
            return new SceneTransitionOptions
            {
                WaitForLoadComplete = true,
                UseFade = true,
                FadeColor = color,
                FadeDuration = duration
            };
        }

        /// <summary>
        /// 씬 로드 완료 후 전환 + 로딩 UI + 페이드 효과 옵션 (풀 옵션)
        /// </summary>
        /// <param name="color">페이드 색상</param>
        /// <param name="duration">페이드 지속 시간</param>
        public static SceneTransitionOptions PreloadedWithLoadingAndFade(Color color, float duration = 0.5f)
        {
            return new SceneTransitionOptions
            {
                WaitForLoadComplete = true,
                ShowLoadingUI = true,
                UseFade = true,
                FadeColor = color,
                FadeDuration = duration
            };
        }
    }
}
