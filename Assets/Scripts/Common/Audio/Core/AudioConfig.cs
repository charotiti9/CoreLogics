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

        [Header("3D 사운드 설정")]
        [Tooltip("3D 사운드 최소 거리 (이 거리까지는 최대 볼륨)")]
        public float spatialMinDistance = 1f;

        [Tooltip("3D 사운드 최대 거리 (이 거리부터 볼륨 0)")]
        public float spatialMaxDistance = 50f;
    }
}
