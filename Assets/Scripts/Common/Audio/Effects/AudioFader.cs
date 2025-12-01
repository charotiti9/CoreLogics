using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 페이드 효과 처리
    /// 페이드 인, 페이드 아웃, 크로스페이드를 구현합니다.
    /// </summary>
    public static class AudioFader
    {
        /// <summary>
        /// 페이드 인
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="targetVolume">목표 볼륨</param>
        /// <param name="duration">페이드 지속 시간</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask FadeInAsync(AudioSource source, float targetVolume, float duration, CancellationToken ct)
        {
            await FadeVolumeAsync(source, 0f, targetVolume, duration, ct);
        }

        /// <summary>
        /// 페이드 아웃
        /// </summary>
        /// <param name="source">AudioSource</param>
        /// <param name="duration">페이드 지속 시간</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask FadeOutAsync(AudioSource source, float duration, CancellationToken ct)
        {
            float currentVolume = source.volume;
            await FadeVolumeAsync(source, currentVolume, 0f, duration, ct);
        }

        /// <summary>
        /// 크로스페이드
        /// </summary>
        /// <param name="from">페이드 아웃할 AudioSource</param>
        /// <param name="to">페이드 인할 AudioSource</param>
        /// <param name="toTargetVolume">페이드 인의 목표 볼륨</param>
        /// <param name="duration">크로스페이드 지속 시간</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask CrossFadeAsync(AudioSource from, AudioSource to, float toTargetVolume, float duration, CancellationToken ct)
        {
            float fromVolume = from.volume;

            // 페이드 아웃과 페이드 인을 동시에 실행
            await UniTask.WhenAll(
                FadeVolumeAsync(from, fromVolume, 0f, duration, ct),
                FadeVolumeAsync(to, 0f, toTargetVolume, duration, ct)
            );
        }

        /// <summary>
        /// 볼륨 페이드 (내부 구현)
        /// </summary>
        private static async UniTask FadeVolumeAsync(AudioSource source, float from, float to, float duration, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                source.volume = to;
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(from, to, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            source.volume = to;
        }
    }
}
