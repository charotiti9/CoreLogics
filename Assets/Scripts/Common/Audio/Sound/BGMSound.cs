using Cysharp.Threading.Tasks;
using Core.Addressable;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// BGM 전용 사운드
    /// 듀얼 AudioSource로 크로스페이드를 GC 없이 구현합니다.
    /// </summary>
    public class BGMSound : MonoBehaviour, IAudioSound
    {
        // ========== 듀얼 AudioSource (크로스페이드용) ==========
        private AudioSource mainAudioSource;
        private AudioSource subAudioSource;

        public AudioSource AudioSource => mainAudioSource;

        // ========== 상태 ==========
        public string CurrentAddress { get; private set; }
        public bool IsPlaying { get; private set; }
        private bool isPaused;

        // ========== 페이드 ==========
        private AudioFader fader;
        private CancellationTokenSource fadeCts;

        // ========== 초기화 ==========

        /// <summary>
        /// BGMSound 초기화
        /// </summary>
        public void Initialize()
        {
            // 듀얼 AudioSource 생성
            mainAudioSource = gameObject.AddComponent<AudioSource>();
            subAudioSource = gameObject.AddComponent<AudioSource>();

            ConfigureAudioSource(mainAudioSource);
            ConfigureAudioSource(subAudioSource);

            fader = new AudioFader();
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;  // 2D 사운드
        }

        // ========== 재생 ==========

        /// <summary>
        /// BGM 재생 (페이드 인 지원)
        /// </summary>
        public async UniTask PlayAsync(string address, AudioClip clip, float volume, float fadeInDuration, CancellationToken ct)
        {
            // 이미 재생 중이면 정지
            if (IsPlaying)
            {
                await StopAsync(fadeInDuration, ct);
            }

            CurrentAddress = address;
            mainAudioSource.clip = clip;
            mainAudioSource.volume = fadeInDuration > 0f ? 0f : volume;
            mainAudioSource.Play();

            IsPlaying = true;
            isPaused = false;

            // 페이드 인
            if (fadeInDuration > 0f)
            {
                fadeCts = new CancellationTokenSource();
                await fader.FadeInAsync(mainAudioSource, volume, fadeInDuration, fadeCts.Token);
            }
        }

        /// <summary>
        /// BGM 정지 (페이드 아웃 지원)
        /// </summary>
        public async UniTask StopAsync(float fadeOutDuration, CancellationToken ct)
        {
            if (!IsPlaying)
                return;

            // 페이드 아웃
            if (fadeOutDuration > 0f)
            {
                fadeCts?.Cancel();
                fadeCts = new CancellationTokenSource();
                await fader.FadeOutAsync(mainAudioSource, fadeOutDuration, fadeCts.Token);
            }

            mainAudioSource.Stop();
            AddressableLoader.Instance.Release(CurrentAddress);

            IsPlaying = false;
            CurrentAddress = null;
        }

        /// <summary>
        /// BGM 일시정지
        /// </summary>
        public void Pause()
        {
            if (IsPlaying && !isPaused)
            {
                mainAudioSource.Pause();
                isPaused = true;
            }
        }

        /// <summary>
        /// BGM 재개
        /// </summary>
        public void Resume()
        {
            if (IsPlaying && isPaused)
            {
                mainAudioSource.UnPause();
                isPaused = false;
            }
        }

        // ========== 크로스페이드 (GC 0) ==========

        /// <summary>
        /// BGM 크로스페이드 전환 (듀얼 AudioSource 핑퐁)
        /// </summary>
        public async UniTask CrossFadeAsync(string newAddress, AudioClip newClip, float volume, float duration, CancellationToken ct)
        {
            // 1. subAudioSource에 새 곡 설정
            subAudioSource.clip = newClip;
            subAudioSource.volume = 0f;
            subAudioSource.Play();

            // 2. 크로스페이드 (main 페이드 아웃 + sub 페이드 인)
            fadeCts?.Cancel();
            fadeCts = new CancellationTokenSource();
            await fader.CrossFadeAsync(mainAudioSource, subAudioSource, volume, duration, fadeCts.Token);

            // 3. AudioSource 교체 (핑퐁)
            mainAudioSource.Stop();
            (mainAudioSource, subAudioSource) = (subAudioSource, mainAudioSource);

            // 4. 이전 클립 언로드
            AddressableLoader.Instance.Release(CurrentAddress);

            CurrentAddress = newAddress;
        }

        // ========== IAudioSound ==========

        public void Play(AudioClip clip, float volume, bool loop)
        {
            mainAudioSource.clip = clip;
            mainAudioSource.volume = volume;
            mainAudioSource.loop = loop;
            mainAudioSource.Play();
            IsPlaying = true;
        }

        public void Stop()
        {
            mainAudioSource.Stop();
            IsPlaying = false;
        }
    }
}
