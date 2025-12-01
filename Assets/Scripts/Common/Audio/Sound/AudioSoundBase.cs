using Core.Addressable;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 모든 오디오 사운드의 기본 클래스
    /// 공통 속성 및 유틸리티를 제공합니다.
    /// </summary>
    public abstract class AudioSoundBase : MonoBehaviour
    {
        /// <summary>
        /// AudioSource 컴포넌트
        /// </summary>
        public abstract AudioSource AudioSource { get; }

        /// <summary>
        /// 현재 재생 중인 Addressable 주소
        /// </summary>
        public string CurrentAddress { get; protected set; }

        /// <summary>
        /// 재생 중인지 여부
        /// </summary>
        public bool IsPlaying { get; protected set; }

        /// <summary>
        /// 사운드 초기화
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 현재 Addressable 리소스 해제
        /// </summary>
        protected void ReleaseCurrentAddress()
        {
            if (!string.IsNullOrEmpty(CurrentAddress))
            {
                AddressableLoader.Instance.Release(CurrentAddress);
                CurrentAddress = null;
            }
        }
    }
}
