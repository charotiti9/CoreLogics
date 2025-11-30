using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 설정 저장/로드 (PlayerPrefs)
    /// </summary>
    public class AudioSettings
    {
        // ========== PlayerPrefs 키 ==========
        private const string MASTER_VOLUME_KEY = "Audio_MasterVolume";
        private const string BGM_VOLUME_KEY = "Audio_BGMVolume";
        private const string SFX_VOLUME_KEY = "Audio_SFXVolume";
        private const string VOICE_VOLUME_KEY = "Audio_VoiceVolume";

        private const string MASTER_MUTE_KEY = "Audio_MasterMute";
        private const string BGM_MUTE_KEY = "Audio_BGMMute";
        private const string SFX_MUTE_KEY = "Audio_SFXMute";
        private const string VOICE_MUTE_KEY = "Audio_VoiceMute";

        // ========== 기본값 ==========
        public const float DEFAULT_VOLUME = 1f;

        // ========== 저장 ==========

        /// <summary>
        /// 오디오 설정 저장
        /// </summary>
        public void Save(float masterVolume, float bgmVolume, float sfxVolume, float voiceVolume,
                         bool masterMute, bool bgmMute, bool sfxMute, bool voiceMute)
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, voiceVolume);

            PlayerPrefs.SetInt(MASTER_MUTE_KEY, masterMute ? 1 : 0);
            PlayerPrefs.SetInt(BGM_MUTE_KEY, bgmMute ? 1 : 0);
            PlayerPrefs.SetInt(SFX_MUTE_KEY, sfxMute ? 1 : 0);
            PlayerPrefs.SetInt(VOICE_MUTE_KEY, voiceMute ? 1 : 0);

            PlayerPrefs.Save();
        }

        // ========== 로드 ==========

        /// <summary>
        /// 볼륨 설정 로드
        /// </summary>
        public (float master, float bgm, float sfx, float voice) LoadVolumes()
        {
            return (
                PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_VOLUME),
                PlayerPrefs.GetFloat(BGM_VOLUME_KEY, DEFAULT_VOLUME),
                PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_VOLUME),
                PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, DEFAULT_VOLUME)
            );
        }

        /// <summary>
        /// 음소거 설정 로드
        /// </summary>
        public (bool master, bool bgm, bool sfx, bool voice) LoadMutes()
        {
            return (
                PlayerPrefs.GetInt(MASTER_MUTE_KEY, 0) == 1,
                PlayerPrefs.GetInt(BGM_MUTE_KEY, 0) == 1,
                PlayerPrefs.GetInt(SFX_MUTE_KEY, 0) == 1,
                PlayerPrefs.GetInt(VOICE_MUTE_KEY, 0) == 1
            );
        }

        // ========== 초기화 ==========

        /// <summary>
        /// 설정 초기화 (기본값으로 리셋)
        /// </summary>
        public void Reset()
        {
            PlayerPrefs.DeleteKey(MASTER_VOLUME_KEY);
            PlayerPrefs.DeleteKey(BGM_VOLUME_KEY);
            PlayerPrefs.DeleteKey(SFX_VOLUME_KEY);
            PlayerPrefs.DeleteKey(VOICE_VOLUME_KEY);

            PlayerPrefs.DeleteKey(MASTER_MUTE_KEY);
            PlayerPrefs.DeleteKey(BGM_MUTE_KEY);
            PlayerPrefs.DeleteKey(SFX_MUTE_KEY);
            PlayerPrefs.DeleteKey(VOICE_MUTE_KEY);

            PlayerPrefs.Save();
        }
    }
}
