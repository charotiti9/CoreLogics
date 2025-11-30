using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 재생 가능 인터페이스
    /// </summary>
    public interface IAudioSound
    {
        /// <summary>
        /// AudioSource 컴포넌트
        /// </summary>
        AudioSource AudioSource { get; }

        /// <summary>
        /// 오디오 재생
        /// </summary>
        /// <param name="clip">재생할 오디오 클립</param>
        /// <param name="volume">볼륨</param>
        /// <param name="loop">루프 재생 여부</param>
        void Play(AudioClip clip, float volume, bool loop);

        /// <summary>
        /// 오디오 정지
        /// </summary>
        void Stop();
    }
}
