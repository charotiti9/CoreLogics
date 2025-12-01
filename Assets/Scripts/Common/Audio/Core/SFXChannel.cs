using Cysharp.Threading.Tasks;
using Core.Addressable;
using Core.Pool;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;

namespace Common.Audio
{
    /// <summary>
    /// SFX 전용 오디오 채널
    /// 2D/3D 사운드, 우선순위 큐, 사운드 풀링을 관리합니다.
    /// </summary>
    public class SFXChannel : AudioChannel
    {
        // 2D 사운드
        private List<SFXSound> active2DSounds;
        private SFXPriorityQueue priorityQueue2D;

        // 3D 사운드
        private List<SpatialSFXSound> active3DSounds;

        /// <summary>
        /// SFXChannel 생성자
        /// </summary>
        /// <param name="maxConcurrent">최대 동시 재생 수 (2D 전용)</param>
        public SFXChannel(int maxConcurrent) : base(AudioChannelType.SFX)
        {
            active2DSounds = new List<SFXSound>(maxConcurrent);
            priorityQueue2D = new SFXPriorityQueue(maxConcurrent);
            active3DSounds = new List<SpatialSFXSound>();
        }

        /// <summary>
        /// 2D SFX 재생 (우선순위 시스템 적용, PoolManager 사용)
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        /// <param name="volume">볼륨 (0.0 ~ 1.0)</param>
        /// <param name="priority">우선순위 (높을수록 우선)</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>재생된 SFXSound 객체. 동시 재생 한도 초과 및 우선순위가 낮아 재생이 거부된 경우 null 반환</returns>
        public async UniTask<SFXSound> Play2DAsync(string address, float volume, int priority, CancellationToken ct)
        {
            // 1. 클립 로드
            var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);

            // 2. PoolManager에서 SFXSound 획득
            var sound = await PoolManager.GetFromPool<SFXSound>(ct);

            // 3. 우선순위 체크
            if (!priorityQueue2D.TryPlay(sound, priority))
            {
                PoolManager.ReturnToPool(sound);
                AddressableLoader.Instance.Release(address);
                return null;
            }

            // 4. 재생
            float finalVolume = GetFinalVolume(volume);
            sound.Play(clip, address, finalVolume, priority);

            // 5. 활성 리스트에 추가
            active2DSounds.Add(sound);

            return sound;
        }

        /// <summary>
        /// 3D SFX 재생 (위치 기반, PoolManager 사용)
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        /// <param name="position">3D 위치</param>
        /// <param name="volume">볼륨 (0.0 ~ 1.0)</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask Play3DAsync(string address, Vector3 position, float volume, CancellationToken ct)
        {
            AudioClip clip = null;
            bool clipLoaded = false;
            SpatialSFXSound spatialSound = null;
            bool soundAcquired = false;

            try
            {
                // 1. 클립 로드
                clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(address, ct);
                clipLoaded = true;

                // 2. PoolManager에서 SpatialSFXSound 획득
                spatialSound = await PoolManager.GetFromPool<SpatialSFXSound>(ct);
                soundAcquired = true;

                // 3. 재생
                spatialSound.transform.position = position;
                spatialSound.PlayAtPosition(clip, address, position, volume);

                active3DSounds.Add(spatialSound);

                // 성공 시 책임 이전
                clipLoaded = false;
                soundAcquired = false;

                // 재생 완료는 Update에서 처리
            }
            catch (OperationCanceledException)
            {
                // 취소는 예외 던지기
                throw;
            }
            catch (System.Exception e)
            {
                Core.Utilities.GameLogger.LogError($"Play3DAsync failed: {e.Message}");
                throw;
            }
            finally
            {
                // 실패 시 리소스 정리
                if (soundAcquired && spatialSound != null)
                {
                    PoolManager.ReturnToPool(spatialSound);
                }

                if (clipLoaded)
                {
                    AddressableLoader.Instance.Release(address);
                }
            }
        }

        /// <summary>
        /// 모든 사운드 정지 (2D + 3D)
        /// </summary>
        public void StopAll()
        {
            // 2D 정지
            foreach (var sound in active2DSounds)
            {
                sound.Stop();
            }
            active2DSounds.Clear();
            priorityQueue2D.Clear();

            // 3D 정지
            foreach (var sound in active3DSounds)
            {
                sound.Stop();
            }
            active3DSounds.Clear();
        }

        /// <summary>
        /// SFX 채널 업데이트
        /// 2D/3D 사운드 완료 체크 및 볼륨 업데이트
        /// </summary>
        public override void Update(float deltaTime)
        {
            // 1. 2D 사운드 완료 체크 (역순 순회)
            for (int i = active2DSounds.Count - 1; i >= 0; i--)
            {
                var sound = active2DSounds[i];

                if (sound.IsPlaying && !sound.AudioSource.isPlaying)
                {
                    // 재생 완료
                    sound.OnPlayComplete();

                    // 우선순위 큐에서 제거
                    priorityQueue2D.Remove(sound);

                    // 활성 리스트에서 제거
                    active2DSounds.RemoveAt(i);

                    // PoolManager로 반환
                    PoolManager.ReturnToPool(sound);
                }
            }

            // 2. 3D 사운드 완료 체크 (역순 순회)
            for (int i = active3DSounds.Count - 1; i >= 0; i--)
            {
                var sound = active3DSounds[i];

                if (sound.CheckHandleCompletion())
                {
                    sound.OnPlayComplete();
                    active3DSounds.RemoveAt(i);
                    PoolManager.ReturnToPool(sound);
                }
            }

            // 3. 볼륨 업데이트 (2D만, Dirty일 때만)
            if (volumeDirty)
            {
                foreach (var sound in active2DSounds)
                {
                    float finalVolume = GetFinalVolume(sound.LocalVolume);
                    sound.AudioSource.volume = finalVolume;
                }

                volumeDirty = false;
            }
        }
    }
}
