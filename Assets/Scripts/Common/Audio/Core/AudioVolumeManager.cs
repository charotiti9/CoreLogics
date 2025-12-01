using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 볼륨, 음소거, 설정 관리 전담
    /// AudioManager로부터 분리하여 단일 책임 원칙 준수
    /// </summary>
    public class AudioVolumeManager
    {
        // ========== 필드 ==========
        private AudioSettings settings;

        // 볼륨
        private float masterVolume = 1f;
        private float bgmVolume = 1f;
        private float sfxVolume = 1f;
        private float voiceVolume = 1f;

        // 음소거
        private bool isMasterMuted = false;
        private bool isBGMMuted = false;
        private bool isSFXMuted = false;
        private bool isVoiceMuted = false;

        // 채널 참조 (볼륨 변경 알림용)
        private AudioChannel bgmChannel;
        private AudioChannel sfxChannel;
        private AudioChannel voiceChannel;

        // ========== 초기화 ==========

        /// <summary>
        /// AudioVolumeManager 초기화
        /// </summary>
        /// <param name="bgm">BGM 채널</param>
        /// <param name="sfx">SFX 채널</param>
        /// <param name="voice">Voice 채널</param>
        public void Initialize(AudioChannel bgm, AudioChannel sfx, AudioChannel voice)
        {
            bgmChannel = bgm;
            sfxChannel = sfx;
            voiceChannel = voice;

            settings = new AudioSettings();
            LoadSettings();
        }

        // ========== 볼륨 프로퍼티 ==========

        /// <summary>
        /// 마스터 볼륨 (0.0 ~ 1.0)
        /// 모든 채널에 영향을 줌
        /// </summary>
        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                if (Mathf.Approximately(masterVolume, value))
                    return;

                masterVolume = Mathf.Clamp01(value);

                // 모든 채널에 볼륨 변경 알림
                bgmChannel?.MarkVolumeDirty();
                sfxChannel?.MarkVolumeDirty();
                voiceChannel?.MarkVolumeDirty();

                SaveSettings();
            }
        }

        /// <summary>
        /// BGM 볼륨 (0.0 ~ 1.0)
        /// </summary>
        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);

                if (bgmChannel != null)
                {
                    bgmChannel.Volume = bgmVolume;
                }

                SaveSettings();
            }
        }

        /// <summary>
        /// SFX 볼륨 (0.0 ~ 1.0)
        /// </summary>
        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);

                if (sfxChannel != null)
                {
                    sfxChannel.Volume = sfxVolume;
                }

                SaveSettings();
            }
        }

        /// <summary>
        /// Voice 볼륨 (0.0 ~ 1.0)
        /// </summary>
        public float VoiceVolume
        {
            get => voiceVolume;
            set
            {
                voiceVolume = Mathf.Clamp01(value);

                if (voiceChannel != null)
                {
                    voiceChannel.Volume = voiceVolume;
                }

                SaveSettings();
            }
        }

        // ========== 음소거 프로퍼티 ==========

        /// <summary>
        /// 마스터 음소거
        /// 모든 채널에 영향을 줌
        /// </summary>
        public bool IsMasterMuted
        {
            get => isMasterMuted;
            set
            {
                if (isMasterMuted == value)
                    return;

                isMasterMuted = value;

                // 모든 채널에 볼륨 변경 알림
                bgmChannel?.MarkVolumeDirty();
                sfxChannel?.MarkVolumeDirty();
                voiceChannel?.MarkVolumeDirty();

                SaveSettings();
            }
        }

        /// <summary>
        /// BGM 음소거
        /// </summary>
        public bool IsBGMMuted
        {
            get => isBGMMuted;
            set
            {
                isBGMMuted = value;

                if (bgmChannel != null)
                {
                    bgmChannel.IsMuted = isBGMMuted;
                }

                SaveSettings();
            }
        }

        /// <summary>
        /// SFX 음소거
        /// </summary>
        public bool IsSFXMuted
        {
            get => isSFXMuted;
            set
            {
                isSFXMuted = value;

                if (sfxChannel != null)
                {
                    sfxChannel.IsMuted = isSFXMuted;
                }

                SaveSettings();
            }
        }

        /// <summary>
        /// Voice 음소거
        /// </summary>
        public bool IsVoiceMuted
        {
            get => isVoiceMuted;
            set
            {
                isVoiceMuted = value;

                if (voiceChannel != null)
                {
                    voiceChannel.IsMuted = isVoiceMuted;
                }

                SaveSettings();
            }
        }

        // ========== 설정 저장/로드 ==========

        /// <summary>
        /// 현재 볼륨 및 음소거 설정을 저장
        /// </summary>
        public void SaveSettings()
        {
            settings.Save(masterVolume, bgmVolume, sfxVolume, voiceVolume,
                         isMasterMuted, isBGMMuted, isSFXMuted, isVoiceMuted);
        }

        /// <summary>
        /// 저장된 볼륨 및 음소거 설정을 로드
        /// </summary>
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
            ApplyToChannels();
        }

        /// <summary>
        /// 설정을 기본값으로 초기화
        /// </summary>
        public void ResetSettings()
        {
            settings.Reset();
            LoadSettings();
        }

        // ========== 내부 메서드 ==========

        /// <summary>
        /// 현재 볼륨 및 음소거 설정을 채널에 적용
        /// </summary>
        private void ApplyToChannels()
        {
            if (bgmChannel != null)
            {
                bgmChannel.Volume = bgmVolume;
                bgmChannel.IsMuted = isBGMMuted;
            }

            if (sfxChannel != null)
            {
                sfxChannel.Volume = sfxVolume;
                sfxChannel.IsMuted = isSFXMuted;
            }

            if (voiceChannel != null)
            {
                voiceChannel.Volume = voiceVolume;
                voiceChannel.IsMuted = isVoiceMuted;
            }
        }
    }
}
