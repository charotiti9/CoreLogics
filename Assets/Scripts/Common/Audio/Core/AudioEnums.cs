namespace Common.Audio
{
    /// <summary>
    /// 오디오 채널 타입
    /// </summary>
    public enum AudioChannelType
    {
        BGM,
        SFX,
        Voice
    }

    /// <summary>
    /// Voice 완료 이유
    /// </summary>
    public enum VoiceCompleteReason
    {
        Completed,  // 정상 완료
        Skipped,    // 스킵됨
        Cancelled   // CancellationToken에 의한 취소
    }
}
