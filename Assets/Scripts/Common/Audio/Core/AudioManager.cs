using Cysharp.Threading.Tasks;
using Core.Addressable;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Core.Utilities;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 시스템의 진입점
    /// BGM, SFX, Voice 채널을 관리하고 통합 API를 제공합니다.
    /// </summary>
    public class AudioManager : EagerMonoSingleton<AudioManager>, IUpdatable
    {

        private AudioChannel bgmChannel;
        private SFXChannel sfxChannel;
        private AudioChannel voiceChannel;

        // 한 번에 하나만 재생되는 것들은 미리 spawn (SFX는 pooling 사용)
        private BGMSound bgmSound;
        private VoiceSound voiceSound;

        private AudioVolume volumeManager;
        [SerializeField] private AudioConfig config;
        public AudioConfig Config => config;

        private bool isInitialized = false;
        private UniTask initializeTask;

        public int UpdateOrder => 10;

        protected override void Initialize()
        {
            base.Initialize();
            initializeTask = InitializeAsync(destroyCancellationToken);
            initializeTask.Forget();
            this.RegisterToGameFlow();  // GameFlowManager에 IUpdatable 등록
        }

        private async UniTask InitializeAsync(CancellationToken ct)
        {
            try
            {
                // 1. Config 로드
                if(config == null)
                {
                    config = await AddressableLoader.Instance.LoadAssetAsync<AudioConfig>("Config/AudioConfig", ct);
                }

                // 2. 채널 생성
                bgmChannel = new AudioChannel(AudioChannelType.BGM);
                sfxChannel = new SFXChannel(config.maxConcurrentSFX);
                voiceChannel = new AudioChannel(AudioChannelType.Voice);

                // 3. 사운드 객체 생성 (런타임 생성)
                bgmSound = CreateBGMSound();
                voiceSound = CreateVoiceSound();

                // 4. 볼륨 매니저 초기화
                volumeManager = new AudioVolume();
                volumeManager.Initialize(bgmChannel, sfxChannel, voiceChannel);

                isInitialized = true;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"AudioManager initialization failed: {e.Message}");
            }
        }

        /// <summary>
        /// 초기화 완료 대기
        /// </summary>
        private async UniTask WaitForInitializeAsync(CancellationToken ct)
        {
            if (!isInitialized)
            {
                await initializeTask.AttachExternalCancellation(ct);
            }
        }

        #region BGM API
        /// <summary>
        /// BGM 재생 (페이드 인 지원)
        /// </summary>
        public async UniTask PlayBGMAsync(string address, float fadeInDuration = 0f, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);

            // 1. 이미 같은 BGM이 재생 중이면 무시
            if (bgmSound.CurrentAddress == address && bgmSound.IsPlaying)
            {
                GameLogger.Log($"BGM already playing: {address}");
                return;
            }

            // 2. 현재 BGM 페이드 아웃
            if (bgmSound.IsPlaying)
            {
                await bgmSound.StopAsync(fadeInDuration, ct);
            }

            AudioClip clip = null;
            bool clipLoaded = false;

            try
            {
                // 3. 새 BGM 로드
                clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);
                clipLoaded = true;

                // 4. 재생 (페이드 인)
                float volume = bgmChannel.GetFinalVolume(BGMVolume);
                await bgmSound.PlayAsync(address, clip, volume, fadeInDuration, ct);

                // 성공 시 clipLoaded를 false로 설정 (Release 책임이 BGMSound로 이전됨)
                clipLoaded = false;

                GameLogger.Log($"BGM started: {address}");
            }
            catch (OperationCanceledException)
            {
                // 취소는 예외 던지기
                throw;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"PlayBGMAsync failed: {e.Message}");
                throw;
            }
            finally
            {
                // 로드는 성공했지만 PlayAsync에서 실패한 경우에만 Release
                if (clipLoaded)
                {
                    AddressableLoader.Instance.Release(address);
                }
            }
        }

        /// <summary>
        /// BGM 정지 (페이드 아웃 지원)
        /// </summary>
        public async UniTask StopBGMAsync(float fadeOutDuration = 0f, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);
            await bgmSound.StopAsync(fadeOutDuration, ct);
        }

        /// <summary>
        /// BGM 일시정지
        /// </summary>
        public void PauseBGM()
        {
            bgmSound.Pause();
        }

        /// <summary>
        /// BGM 재개
        /// </summary>
        public void ResumeBGM()
        {
            bgmSound.Resume();
        }

        /// <summary>
        /// BGM 크로스페이드 전환
        /// </summary>
        public async UniTask CrossFadeBGMAsync(string newAddress, float duration, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);

            AudioClip newClip = null;
            bool clipLoaded = false;

            try
            {
                // 1. 새 BGM 로드
                newClip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(newAddress, ct);
                clipLoaded = true;

                // 2. 크로스페이드 실행
                float volume = bgmChannel.GetFinalVolume(BGMVolume);
                await bgmSound.CrossFadeAsync(newAddress, newClip, volume, duration, ct);

                // 성공 시 clipLoaded를 false로 설정 (Release 책임이 BGMSound로 이전됨)
                clipLoaded = false;
            }
            catch (OperationCanceledException)
            {
                // 취소는 예외 던지기
                throw;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"CrossFadeBGMAsync failed: {e.Message}");
                throw;
            }
            finally
            {
                // 로드는 성공했지만 CrossFadeAsync에서 실패한 경우에만 Release
                if (clipLoaded)
                {
                    AddressableLoader.Instance.Release(newAddress);
                }
            }
        }
        #endregion

        #region SFX API

        /// <summary>
        /// SFX 재생 (2D)
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        /// <param name="volume">볼륨 (0.0 ~ 1.0)</param>
        /// <param name="priority">우선순위 (높을수록 우선, 기본값 128)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>재생된 SFXSound 객체. 우선순위가 낮아 재생이 거부된 경우 null 반환</returns>
        public async UniTask<SFXSound> PlaySFXAsync(string address, float volume = 1f, int priority = 128, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);
            return await sfxChannel.Play2DAsync(address, volume, priority, ct);
        }

        /// <summary>
        /// SFX 재생 (3D 위치)
        /// </summary>
        public async UniTask PlaySFXAtPositionAsync(string address, Vector3 position, float volume = 1f, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);
            await sfxChannel.Play3DAsync(address, position, volume, ct);
        }

        /// <summary>
        /// 모든 SFX 정지
        /// </summary>
        public void StopAllSFX()
        {
            sfxChannel.StopAll();
        }

        #endregion

        #region Voice API

        /// <summary>
        /// Voice 재생
        /// </summary>
        public async UniTask PlayVoiceAsync(string address, CancellationToken ct = default)
        {
            await WaitForInitializeAsync(ct);

            bool clipLoaded = false;

            try
            {
                // 1. 클립 로드
                AudioClip clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);
                clipLoaded = true;

                // 2. 재생
                float volume = voiceChannel.GetFinalVolume(VoiceVolume);
                await voiceSound.PlayAsync(address, clip, volume, ct);

                // 성공 시 책임 이전 (VoiceSound가 Release 담당)
                clipLoaded = false;
            }
            catch (OperationCanceledException)
            {
                // 취소는 예외 던지기
                throw;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"PlayVoiceAsync failed: {e.Message}");
                throw;
            }
            finally
            {
                // 로드는 성공했지만 PlayAsync에서 실패한 경우에만 Release
                if (clipLoaded)
                {
                    AddressableLoader.Instance.Release(address);
                }
            }
        }

        /// <summary>
        /// Voice 스킵
        /// </summary>
        public void SkipVoice()
        {
            voiceSound.Skip();
        }

        /// <summary>
        /// Voice 완료 대기 (스킵 여부 반환)
        /// </summary>
        public async UniTask<VoiceCompleteReason> WaitForVoiceCompleteAsync(CancellationToken ct = default)
        {
            return await voiceSound.WaitForCompleteAsync(ct);
        }

        #endregion

        #region 볼륨 제어

        public float MasterVolume
        {
            get => volumeManager.MasterVolume;
            set => volumeManager.MasterVolume = value;
        }

        public float BGMVolume
        {
            get => volumeManager.BGMVolume;
            set => volumeManager.BGMVolume = value;
        }

        public float SFXVolume
        {
            get => volumeManager.SFXVolume;
            set => volumeManager.SFXVolume = value;
        }

        public float VoiceVolume
        {
            get => volumeManager.VoiceVolume;
            set => volumeManager.VoiceVolume = value;
        }

        public bool IsMasterMuted
        {
            get => volumeManager.IsMasterMuted;
            set => volumeManager.IsMasterMuted = value;
        }

        public bool IsBGMMuted
        {
            get => volumeManager.IsBGMMuted;
            set => volumeManager.IsBGMMuted = value;
        }

        public bool IsSFXMuted
        {
            get => volumeManager.IsSFXMuted;
            set => volumeManager.IsSFXMuted = value;
        }

        public bool IsVoiceMuted
        {
            get => volumeManager.IsVoiceMuted;
            set => volumeManager.IsVoiceMuted = value;
        }

        public void SaveSettings()
        {
            volumeManager.SaveSettings();
        }

        public void LoadSettings()
        {
            volumeManager.LoadSettings();
        }

        public void ResetSettings()
        {
            volumeManager.ResetSettings();
        }

        #endregion

        #region 플레이리스트

        /// <summary>
        /// 오디오 플레이리스트 생성
        /// </summary>
        public AudioPlaylist CreatePlaylist(List<string> addresses, AudioPlaylist.PlayMode mode)
        {
            return new AudioPlaylist(addresses, mode);
        }

        #endregion

        public void OnUpdate(float deltaTime)
        {
            if (bgmChannel == null)
                return;

            bgmChannel.Update(deltaTime);
            sfxChannel.Update(deltaTime);
            voiceChannel.Update(deltaTime);
        }

        public void Dispose()
        {
            this.UnregisterFromGameFlow();  // GameFlowManager에서 등록 해제
            SaveSettings();
        }

        private BGMSound CreateBGMSound()
        {
            var go = new GameObject("BGMSound");
            go.transform.SetParent(transform);
            var sound = go.AddComponent<BGMSound>();
            sound.Initialize();
            return sound;
        }

        private VoiceSound CreateVoiceSound()
        {
            var go = new GameObject("VoiceSound");
            go.transform.SetParent(transform);
            var sound = go.AddComponent<VoiceSound>();
            sound.Initialize();
            return sound;
        }
    }
}
