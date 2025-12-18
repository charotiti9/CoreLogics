using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Pool;
using System.Threading;
using UnityEngine;
using Core.Utilities;

namespace Common.Audio
{
    /// <summary>
    /// SFX 사운드 (오브젝트 풀링)
    /// 효과음을 재생하고 완료 시 자동으로 풀에 반환됩니다.
    /// </summary>
    [PoolAddress("Audio/SFXSound", "AudioSounds")]
    public class SFXSound : AudioSoundBase, IPoolable
    {
        private AudioSource audioSource;
        public override AudioSource AudioSource => audioSource;

        public float LocalVolume { get; private set; }

        private UniTaskCompletionSource completionSource;

        /// <summary>
        /// SFXSound 초기화 (Prefab의 AudioSource 컴포넌트 참조)
        /// </summary>
        public override void Initialize()
        {
            // AudioSource는 Prefab에 미리 포함되어 있음
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                GameLogger.LogError("[SFXSound] AudioSource 컴포넌트가 없습니다. Prefab에 AudioSource를 추가해주세요.");
            }
        }

        /// <summary>
        /// SFX 재생
        /// </summary>
        /// <param name="clip">재생할 오디오 클립</param>
        /// <param name="address">Addressable 주소</param>
        /// <param name="volume">볼륨</param>
        /// <param name="priority">우선순위</param>
        public void Play(AudioClip clip, string address, float volume, int priority)
        {
            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.Play();

            CurrentAddress = address;
            LocalVolume = volume;
            IsPlaying = true;

            completionSource = new UniTaskCompletionSource();
        }

        /// <summary>
        /// SFX 정지
        /// </summary>
        public void Stop()
        {
            if (!IsPlaying)
                return;

            AudioSource.Stop();
            OnPlayComplete();
        }

        /// <summary>
        /// 재생 완료까지 대기
        /// </summary>
        public async UniTask WaitForCompleteAsync(CancellationToken ct)
        {
            if (completionSource == null)
                return;

            await completionSource.Task.AttachExternalCancellation(ct);
        }

        /// <summary>
        /// 재생 완료 처리
        /// </summary>
        public void OnPlayComplete()
        {
            IsPlaying = false;

            // 완료 알림
            completionSource?.TrySetResult();

            // 주의: Addressable 리소스 해제는 OnReturnToPool에서 처리
        }

        public void OnGetFromPool()
        {
            // InstanceLifecycle.Activate()가 자동으로 SetActive(true) 호출
            // 추가 초기화가 필요한 경우 여기서 수행
        }

        public void OnReturnToPool()
        {
            // InstanceLifecycle.Deactivate()가 자동으로 SetActive(false) 호출
            // 정리 작업만 수행

            if (AudioSource != null)
            {
                AudioSource.Stop();
                AudioSource.clip = null;
            }

            IsPlaying = false;
            completionSource = null;

            // Addressable 리소스 해제
            ReleaseCurrentAddress();
        }
    }
}
