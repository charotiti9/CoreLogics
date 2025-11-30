using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 시스템 설정 (ScriptableObject)
    /// 프로젝트별 오디오 시스템 동작을 커스터마이징합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("동시 재생 제한")]
        [Tooltip("동시 재생 가능한 최대 BGM 수 (보통 1)")]
        public int maxConcurrentBGM = 1;

        [Tooltip("동시 재생 가능한 최대 SFX 수")]
        public int maxConcurrentSFX = 10;

        [Tooltip("동시 재생 가능한 최대 Voice 수 (보통 1)")]
        public int maxConcurrentVoice = 1;

        [Header("SFX 풀링 설정")]
        [Tooltip("SFX 사운드 초기 풀 크기")]
        public int initialSFXPoolSize = 5;

        [Header("일시정지 설정")]
        [Tooltip("게임 일시정지 시 BGM 일시정지 여부")]
        public bool pauseBGMOnGamePause = false;

        [Tooltip("게임 일시정지 시 SFX 일시정지 여부")]
        public bool pauseSFXOnGamePause = true;

        [Header("씬 전환 설정")]
        [Tooltip("씬 전환 시 모든 SFX 정지 여부")]
        public bool stopSFXOnSceneChange = true;

        [Tooltip("씬 전환 시 BGM 유지 여부 (같은 주소인 경우)")]
        public bool keepBGMOnSceneChange = true;
    }
}
