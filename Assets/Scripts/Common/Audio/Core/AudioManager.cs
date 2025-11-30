using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Pool;
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
        // ========== 채널 ==========
        private AudioChannel bgmChannel;
        private AudioChannel sfxChannel;
        private AudioChannel voiceChannel;

        // ========== 사운드 객체 ==========
        private BGMSound bgmSound;
        private VoiceSound voiceSound;

        // ========== 설정 ==========
        private AudioSettings settings;
        private AudioConfig config;
        public AudioConfig Config => config;

        // ========== 3D 사운드 관리 ==========
        private List<SpatialSFXSound> activeSpatialSounds = new List<SpatialSFXSound>();

        // ========== 초기화 ==========

        protected override void Initialize()
        {
            base.Initialize();
            InitializeAsync(destroyCancellationToken).Forget();
            this.RegisterToGameFlow();  // GameFlowManager에 IUpdatable 등록
        }

        private async UniTask InitializeAsync(CancellationToken ct)
        {
            try
            {
                // 1. Config 로드
                config = await AddressableLoader.Instance.LoadAssetAsync<AudioConfig>("Config/AudioConfig", ct);

                // 2. 설정 로드
                settings = new AudioSettings();
                LoadSettings();

                // 3. 채널 생성
                bgmChannel = new AudioChannel(AudioChannelType.BGM, config.maxConcurrentBGM);
                sfxChannel = new AudioChannel(AudioChannelType.SFX, config.maxConcurrentSFX);
                voiceChannel = new AudioChannel(AudioChannelType.Voice, config.maxConcurrentVoice);

                // 4. 사운드 객체 생성 (런타임 생성)
                bgmSound = CreateBGMSound();
                voiceSound = CreateVoiceSound();

                // 5. 채널 초기화 (PoolManager 사용으로 풀 생성 불필요)
                sfxChannel.Initialize(config.initialSFXPoolSize, transform);

                GameLogger.Log("AudioManager initialized");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"AudioManager initialization failed: {e.Message}");
            }
        }

        // ========== BGM API ==========

        /// <summary>
        /// BGM 재생 (페이드 인 지원)
        /// </summary>
        public async UniTask PlayBGMAsync(string address, float fadeInDuration = 0f, CancellationToken ct = default)
        {
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

            // 3. 새 BGM 로드
            var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);

            // 4. 재생 (페이드 인)
            float volume = bgmChannel.GetFinalVolume(BGMVolume);
            await bgmSound.PlayAsync(address, clip, volume, fadeInDuration, ct);

            GameLogger.Log($"BGM started: {address}");
        }

        /// <summary>
        /// BGM 정지 (페이드 아웃 지원)
        /// </summary>
        public async UniTask StopBGMAsync(float fadeOutDuration = 0f, CancellationToken ct = default)
        {
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
            // 1. 새 BGM 로드
            var newClip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(newAddress, ct);

            // 2. 크로스페이드 실행
            float volume = bgmChannel.GetFinalVolume(BGMVolume);
            await bgmSound.CrossFadeAsync(newAddress, newClip, volume, duration, ct);
        }

        // ========== SFX API ==========

        /// <summary>
        /// SFX 재생 (2D)
        /// </summary>
        public async UniTask<SFXSound> PlaySFXAsync(string address, float volume = 1f, int priority = 128, CancellationToken ct = default)
        {
            return await sfxChannel.PlaySFXAsync(address, volume, priority, ct);
        }

        /// <summary>
        /// SFX 재생 (3D 위치, PoolManager 사용)
        /// </summary>
        public async UniTask PlaySFXAtPositionAsync(string address, Vector3 position, float volume = 1f, CancellationToken ct = default)
        {
            var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);

            // PoolManager에서 SpatialSFXSound 획득
            SpatialSFXSound spatialSound = await PoolManager.GetFromPool<SpatialSFXSound>(ct);
            spatialSound.transform.position = position;
            spatialSound.PlayAtPosition(clip, address, position, volume);

            activeSpatialSounds.Add(spatialSound);

            // 재생 완료 후 자동 반환
            WaitAndReturnSpatialSoundAsync(spatialSound, ct).Forget();
        }

        /// <summary>
        /// 모든 SFX 정지
        /// </summary>
        public void StopAllSFX()
        {
            sfxChannel.StopAll();
        }

        // ========== Voice API ==========

        /// <summary>
        /// Voice 재생
        /// </summary>
        public async UniTask PlayVoiceAsync(string address, CancellationToken ct = default)
        {
            var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);
            float volume = voiceChannel.GetFinalVolume(VoiceVolume);
            await voiceSound.PlayAsync(address, clip, volume, ct);
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

        // ========== 볼륨 제어 ==========

        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private float voiceVolume = 1f;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                SaveSettings();
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                bgmChannel.Volume = bgmVolume;
                SaveSettings();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                sfxChannel.Volume = sfxVolume;
                SaveSettings();
            }
        }

        public float VoiceVolume
        {
            get => voiceVolume;
            set
            {
                voiceVolume = Mathf.Clamp01(value);
                voiceChannel.Volume = voiceVolume;
                SaveSettings();
            }
        }

        // ========== 음소거 ==========

        private bool isMasterMuted = false;
        private bool isBGMMuted = false;
        private bool isSFXMuted = false;
        private bool isVoiceMuted = false;

        public bool IsMasterMuted
        {
            get => isMasterMuted;
            set
            {
                isMasterMuted = value;
                SaveSettings();
            }
        }

        public bool IsBGMMuted
        {
            get => isBGMMuted;
            set
            {
                isBGMMuted = value;
                bgmChannel.IsMuted = isBGMMuted;
                SaveSettings();
            }
        }

        public bool IsSFXMuted
        {
            get => isSFXMuted;
            set
            {
                isSFXMuted = value;
                sfxChannel.IsMuted = isSFXMuted;
                SaveSettings();
            }
        }

        public bool IsVoiceMuted
        {
            get => isVoiceMuted;
            set
            {
                isVoiceMuted = value;
                voiceChannel.IsMuted = isVoiceMuted;
                SaveSettings();
            }
        }

        // ========== 설정 저장/로드 ==========

        public void SaveSettings()
        {
            settings.Save(masterVolume, bgmVolume, sfxVolume, voiceVolume,
                         isMasterMuted, isBGMMuted, isSFXMuted, isVoiceMuted);
        }

        public void LoadSettings()
        {
            var (master, bgm, sfx, voice) = settings.LoadVolumes();
            var (masterMute, bgmMute, sfxMute, voiceMute) = settings.LoadMutes();

            masterVolume = master;
            bgmVolume = bgm;
            sfxVolume = sfx;
            voiceVolume = voice;

            isMasterMuted = masterMute;
            isBGMMuted = bgmMute;
            isSFXMuted = sfxMute;
            isVoiceMuted = voiceMute;

            // 채널에 적용
            if (bgmChannel != null)
            {
                bgmChannel.Volume = bgmVolume;
                sfxChannel.Volume = sfxVolume;
                voiceChannel.Volume = voiceVolume;

                bgmChannel.IsMuted = isBGMMuted;
                sfxChannel.IsMuted = isSFXMuted;
                voiceChannel.IsMuted = isVoiceMuted;
            }
        }

        public void ResetSettings()
        {
            settings.Reset();
            LoadSettings();
        }

        // ========== 플레이리스트 생성 ==========

        /// <summary>
        /// 오디오 플레이리스트 생성
        /// </summary>
        public AudioPlaylist CreatePlaylist(List<string> addresses, AudioPlaylist.PlayMode mode = AudioPlaylist.PlayMode.Random)
        {
            return new AudioPlaylist(addresses, mode);
        }

        // ========== IUpdatable ==========

        public int UpdateOrder => 10;

        public void OnUpdate(float deltaTime)
        {
            if (bgmChannel == null)
                return;

            bgmChannel.Update(deltaTime);
            sfxChannel.Update(deltaTime);
            voiceChannel.Update(deltaTime);

            // 3D 사운드 완료 체크
            for (int i = activeSpatialSounds.Count - 1; i >= 0; i--)
            {
                var sound = activeSpatialSounds[i];

                if (sound.IsPlaying && !sound.AudioSource.isPlaying)
                {
                    sound.OnPlayComplete();
                    activeSpatialSounds.RemoveAt(i);
                    PoolManager.ReturnToPool(sound);  // PoolManager로 반환
                }
            }
        }

        public void Dispose()
        {
            this.UnregisterFromGameFlow();  // GameFlowManager에서 등록 해제
            SaveSettings();
        }

        // ========== 런타임 생성 ==========

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

        private async UniTaskVoid WaitAndReturnSpatialSoundAsync(SpatialSFXSound sound, CancellationToken ct)
        {
            try
            {
                await sound.WaitForCompleteAsync(ct);
            }
            catch (OperationCanceledException) { }
            finally
            {
                activeSpatialSounds.Remove(sound);
                PoolManager.ReturnToPool(sound);  // PoolManager로 반환
            }
        }
    }
}
