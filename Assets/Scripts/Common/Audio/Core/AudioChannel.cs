using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 채널 기본 클래스
    /// 볼륨, 음소거 등 공통 기능을 제공합니다.
    /// </summary>
    public class AudioChannel : IAudioChannel
    {
        public AudioChannelType ChannelType { get; }

        private float volume;
        public float Volume
        {
            get => volume;
            set
            {
                if (Mathf.Approximately(volume, value))
                    return;

                volume = value;
                volumeDirty = true;
            }
        }

        private bool isMuted;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                if (isMuted == value)
                    return;

                isMuted = value;
                volumeDirty = true;
            }
        }

        protected bool volumeDirty = true;  // 초기에는 true, 파생 클래스에서 접근 가능


        public AudioChannel(AudioChannelType type)
        {
            ChannelType = type;
            volume = 1f;
            isMuted = false;
        }

        /// <summary>
        /// 채널 업데이트 (파생 클래스에서 오버라이드 가능)
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            // 기본 구현: 아무것도 하지 않음
            // SFXChannel에서 오버라이드하여 사운드 관리
        }


        /// <summary>
        /// 볼륨 변경 플래그 설정 (외부에서 호출)
        /// </summary>
        public void MarkVolumeDirty()
        {
            volumeDirty = true;
        }


        /// <summary>
        /// 최종 볼륨 계산 (Master * Channel * Local)
        /// </summary>
        public float GetFinalVolume(float localVolume)
        {
            if (IsMuted || AudioManager.Instance.IsMasterMuted)
                return 0f;

            return AudioManager.Instance.MasterVolume * Volume * localVolume;
        }
    }
}
