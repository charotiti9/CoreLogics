using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Pool;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 채널 (BGM, SFX, Voice)
    /// 각 채널별 재생, 볼륨, 음소거를 독립적으로 관리합니다.
    /// </summary>
    public class AudioChannel : IAudioChannel
    {
        // ========== 속성 ==========
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

        private int maxConcurrentSounds;

        // ========== 재생 관리 ==========
        private List<SFXSound> activeSounds;
        private AudioPriorityQueue priorityQueue;

        // ========== 볼륨 최적화 ==========
        private bool volumeDirty = true;  // 초기에는 true

        // ========== 초기화 ==========

        public AudioChannel(AudioChannelType type, int maxConcurrent)
        {
            ChannelType = type;
            maxConcurrentSounds = maxConcurrent;
            volume = 1f;
            isMuted = false;

            if (type == AudioChannelType.SFX)
            {
                activeSounds = new List<SFXSound>(maxConcurrent);
                priorityQueue = new AudioPriorityQueue(maxConcurrent);
            }
        }

        /// <summary>
        /// 채널 초기화 (PoolManager 사용으로 풀 생성 불필요)
        /// </summary>
        public void Initialize(int poolSize, Transform parent)
        {
            // PoolManager가 자동으로 풀을 관리하므로 별도 초기화 불필요
            // maxSize는 PoolStorage에서 기본값(10) 사용
        }

        // ========== 재생 제어 ==========

        /// <summary>
        /// SFX 재생 (우선순위 시스템 적용, PoolManager 사용)
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        /// <param name="volume">볼륨 (0.0 ~ 1.0)</param>
        /// <param name="priority">우선순위 (높을수록 우선)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>재생된 SFXSound 객체. 동시 재생 한도 초과 및 우선순위가 낮아 재생이 거부된 경우 null 반환</returns>
        public async UniTask<SFXSound> PlaySFXAsync(string address, float volume, int priority, CancellationToken ct)
        {
            // 1. 클립 로드
            var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);

            // 2. PoolManager에서 SFXSound 획득
            var sound = await PoolManager.GetFromPool<SFXSound>(ct);

            // 3. 우선순위 체크
            if (!priorityQueue.TryPlay(sound, priority))
            {
                PoolManager.ReturnToPool(sound);
                AddressableLoader.Instance.Release(address);
                return null;
            }

            // 4. 재생
            float finalVolume = GetFinalVolume(volume);
            sound.Play(clip, address, finalVolume, priority);

            // 5. 활성 리스트에 추가
            activeSounds.Add(sound);

            return sound;
        }

        /// <summary>
        /// 모든 사운드 정지
        /// </summary>
        public void StopAll()
        {
            foreach (var sound in activeSounds)
            {
                sound.Stop();
            }
            activeSounds.Clear();
            priorityQueue.Clear();
        }

        // ========== 업데이트 (GameFlowManager → AudioManager → AudioChannel) ==========

        public void Update(float deltaTime)
        {
            if (ChannelType != AudioChannelType.SFX)
                return;

            // 1. 완료된 사운드 체크 (역순 순회)
            for (int i = activeSounds.Count - 1; i >= 0; i--)
            {
                var sound = activeSounds[i];

                if (sound.IsPlaying && !sound.AudioSource.isPlaying)
                {
                    // 재생 완료
                    sound.OnPlayComplete();

                    // 우선순위 큐에서 제거
                    priorityQueue.Remove(sound);

                    // 활성 리스트에서 제거
                    activeSounds.RemoveAt(i);

                    // PoolManager로 반환
                    PoolManager.ReturnToPool(sound);
                }
            }

            // 2. 볼륨 업데이트 (Dirty일 때만)
            if (volumeDirty)
            {
                foreach (var sound in activeSounds)
                {
                    float finalVolume = GetFinalVolume(sound.LocalVolume);
                    sound.AudioSource.volume = finalVolume;
                }

                volumeDirty = false;
            }
        }

        // ========== 볼륨 제어 ==========

        /// <summary>
        /// 볼륨 변경 플래그 설정 (외부에서 호출)
        /// </summary>
        public void MarkVolumeDirty()
        {
            volumeDirty = true;
        }

        // ========== 볼륨 계산 ==========

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
