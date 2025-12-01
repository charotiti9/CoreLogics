using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Utilities;
using System;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// Voice 사운드
    /// 음성/대사 재생 및 스킵 기능을 제공합니다.
    /// </summary>
    public class VoiceSound : AudioSoundBase
    {
        private AudioSource audioSource;
        public override AudioSource AudioSource => audioSource;

        private UniTaskCompletionSource<VoiceCompleteReason> completionSource;

        /// <summary>
        /// VoiceSound 초기화
        /// </summary>
        public override void Initialize()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        /// <summary>
        /// Voice 재생
        /// </summary>
        public UniTask PlayAsync(string address, AudioClip clip, float volume, CancellationToken ct)
        {
            // 이전 Voice 정지
            if (IsPlaying)
            {
                Stop();
            }

            CurrentAddress = address;
            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.Play();

            IsPlaying = true;
            completionSource = new UniTaskCompletionSource<VoiceCompleteReason>();

            // 완료 체크 (별도 Task로)
            MonitorPlaybackAsync(ct).Forget();

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 재생 상태 모니터링
        /// </summary>
        private async UniTaskVoid MonitorPlaybackAsync(CancellationToken ct)
        {
            try
            {
                // 재생 완료까지 대기
                while (AudioSource.isPlaying && IsPlaying)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                // 정상 완료
                if (IsPlaying)
                {
                    OnPlayComplete(VoiceCompleteReason.Completed);
                }
            }
            catch (OperationCanceledException)
            {
                OnPlayComplete(VoiceCompleteReason.Cancelled);
            }
            catch (Exception e)
            {
                GameLogger.LogError($"VoiceSound playback monitoring failed: {e.Message}");
                OnPlayComplete(VoiceCompleteReason.Cancelled);
            }
        }

        /// <summary>
        /// Voice 정지
        /// </summary>
        public void Stop()
        {
            if (!IsPlaying)
                return;

            AudioSource.Stop();
            OnPlayComplete(VoiceCompleteReason.Completed);
        }

        /// <summary>
        /// Voice 스킵 (플레이어 입력)
        /// </summary>
        public void Skip()
        {
            if (!IsPlaying)
                return;

            AudioSource.Stop();
            OnPlayComplete(VoiceCompleteReason.Skipped);
        }

        /// <summary>
        /// Voice 완료까지 대기 (스킵 여부 반환)
        /// </summary>
        public async UniTask<VoiceCompleteReason> WaitForCompleteAsync(CancellationToken ct)
        {
            if (completionSource == null)
                return VoiceCompleteReason.Completed;

            return await completionSource.Task.AttachExternalCancellation(ct);
        }

        private void OnPlayComplete(VoiceCompleteReason reason)
        {
            IsPlaying = false;

            completionSource?.TrySetResult(reason);

            ReleaseCurrentAddress();
        }
    }
}
