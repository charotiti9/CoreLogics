using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Pool;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// SFX 사운드 (오브젝트 풀링)
    /// 효과음을 재생하고 완료 시 자동으로 풀에 반환됩니다.
    /// </summary>
    [PoolAddress("Audio/SFXSound", "AudioSounds")]
    public class SFXSound : MonoBehaviour, IPoolable, IAudioSound
    {
        // ========== 컴포넌트 ==========
        public AudioSource AudioSource { get; private set; }

        // ========== 상태 ==========
        private string currentAddress;
        private int priority;
        public bool IsPlaying { get; private set; }
        public float LocalVolume { get; private set; }

        // ========== 완료 콜백 ==========
        private UniTaskCompletionSource completionSource;

        // ========== 초기화 ==========

        /// <summary>
        /// SFXSound 초기화 (Prefab의 AudioSource 컴포넌트 참조)
        /// </summary>
        public void Initialize()
        {
            // AudioSource는 Prefab에 미리 포함되어 있음
            AudioSource = GetComponent<AudioSource>();

            if (AudioSource == null)
            {
                Debug.LogError("[SFXSound] AudioSource 컴포넌트가 없습니다. Prefab에 AudioSource를 추가해주세요.");
            }
        }

        // ========== 재생 ==========

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

            currentAddress = address;
            this.priority = priority;
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

        // ========== 완료 대기 ==========

        /// <summary>
        /// 재생 완료까지 대기
        /// </summary>
        public async UniTask WaitForCompleteAsync(CancellationToken ct)
        {
            if (completionSource == null)
                return;

            await completionSource.Task.AttachExternalCancellation(ct);
        }

        // ========== 완료 처리 (AudioChannel에서 호출) ==========

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

        // ========== IPoolable ==========

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
            if (!string.IsNullOrEmpty(currentAddress))
            {
                AddressableLoader.Instance.Release(currentAddress);
                currentAddress = null;
            }
        }

        // ========== IAudioSound ==========

        void IAudioSound.Play(AudioClip clip, float volume, bool loop)
        {
            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.loop = loop;
            AudioSource.Play();
            IsPlaying = true;
        }
    }
}
