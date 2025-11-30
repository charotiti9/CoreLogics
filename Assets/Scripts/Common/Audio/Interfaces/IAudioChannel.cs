namespace Common.Audio
{
    /// <summary>
    /// 오디오 채널 인터페이스
    /// </summary>
    public interface IAudioChannel
    {
        /// <summary>
        /// 채널 타입
        /// </summary>
        AudioChannelType ChannelType { get; }

        /// <summary>
        /// 채널 볼륨
        /// </summary>
        float Volume { get; set; }

        /// <summary>
        /// 음소거 여부
        /// </summary>
        bool IsMuted { get; set; }

        /// <summary>
        /// 업데이트
        /// </summary>
        /// <param name="deltaTime">델타 타임</param>
        void Update(float deltaTime);

        /// <summary>
        /// 최종 볼륨 계산 (Master * Channel * Local)
        /// </summary>
        /// <param name="localVolume">로컬 볼륨</param>
        /// <returns>최종 볼륨</returns>
        float GetFinalVolume(float localVolume);
    }
}
