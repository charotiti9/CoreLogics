using Core.Addressable;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 3D 공간 오디오 (Spatial SFX)
    /// </summary>
    [PoolAddress("Audio/SpatialSFXSound", "AudioSounds")]
    public class SpatialSFXSound : MonoBehaviour, IPoolable, IAudioSound
    {
        public AudioSource AudioSource { get; private set; }

        private string currentAddress;
        public bool IsPlaying { get; private set; }
        public float LocalVolume { get; private set; }

        private UniTaskCompletionSource completionSource;

        // ========== 초기화 ==========

        /// <summary>
        /// SpatialSFXSound 초기화 (Prefab의 AudioSource 컴포넌트 참조)
        /// </summary>
        public void Initialize()
        {
            // AudioSource는 Prefab에 미리 포함되어 있음
            AudioSource = GetComponent<AudioSource>();

            if (AudioSource == null)
            {
                Debug.LogError("[SpatialSFXSound] AudioSource 컴포넌트가 없습니다. Prefab에 AudioSource를 추가해주세요.");
            }
        }

        // ========== 재생 ==========

        /// <summary>
        /// 3D 위치에서 재생
        /// </summary>
        public void PlayAtPosition(AudioClip clip, string address, Vector3 position, float volume)
        {
            transform.position = position;

            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.Play();

            currentAddress = address;
            LocalVolume = volume;
            IsPlaying = true;

            completionSource = new UniTaskCompletionSource();
        }

        /// <summary>
        /// 정지
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

        // ========== 완료 처리 ==========

        /// <summary>
        /// 재생 완료 처리
        /// </summary>
        public void OnPlayComplete()
        {
            IsPlaying = false;
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
