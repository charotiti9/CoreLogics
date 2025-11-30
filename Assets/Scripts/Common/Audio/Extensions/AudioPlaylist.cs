using Core.Utilities;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Common.Audio
{
    /// <summary>
    /// 오디오 플레이리스트
    /// BGM, SFX 모두 사용 가능한 범용 플레이리스트
    /// </summary>
    public class AudioPlaylist
    {
        private List<string> addresses;
        private int currentIndex;
        private PlayMode playMode;
        private List<int> shuffleIndices;

        public enum PlayMode
        {
            Sequential,  // 순차 재생
            Random,      // 랜덤 재생
            Shuffle      // 셔플 (전체 재생 후 다시 셔플)
        }

        // ========== 초기화 ==========

        public AudioPlaylist(List<string> addresses, PlayMode mode = PlayMode.Random)
        {
            if (addresses == null || addresses.Count == 0)
            {
                GameLogger.LogError("AudioPlaylist: addresses is null or empty");
                this.addresses = new List<string>();
                return;
            }

            this.addresses = new List<string>(addresses);
            playMode = mode;
            currentIndex = 0;

            if (mode == PlayMode.Shuffle)
            {
                ShufflePlaylist();
            }
        }

        // ========== BGM 재생 ==========

        /// <summary>
        /// 다음 BGM 재생 (크로스페이드)
        /// </summary>
        public async UniTask PlayNextBGMAsync(float crossFadeDuration = 2f, CancellationToken ct = default)
        {
            string address = GetNext();
            if (address == null)
                return;

            await AudioManager.Instance.CrossFadeBGMAsync(address, crossFadeDuration, ct);
        }

        /// <summary>
        /// 이전 BGM 재생 (크로스페이드)
        /// </summary>
        public async UniTask PlayPreviousBGMAsync(float crossFadeDuration = 2f, CancellationToken ct = default)
        {
            string address = GetPrevious();
            if (address == null)
                return;

            await AudioManager.Instance.CrossFadeBGMAsync(address, crossFadeDuration, ct);
        }

        /// <summary>
        /// 특정 인덱스의 BGM 재생
        /// </summary>
        public async UniTask PlayBGMAtIndexAsync(int index, float crossFadeDuration = 2f, CancellationToken ct = default)
        {
            if (index < 0 || index >= addresses.Count)
            {
                GameLogger.LogError($"Invalid index: {index}");
                return;
            }

            currentIndex = index;
            await AudioManager.Instance.CrossFadeBGMAsync(addresses[index], crossFadeDuration, ct);
        }

        // ========== SFX 재생 ==========

        /// <summary>
        /// 플레이리스트에서 SFX 재생 (2D)
        /// </summary>
        public async UniTask<SFXSound> PlaySFXAsync(float volume = 1f, int priority = 128, CancellationToken ct = default)
        {
            string address = GetNext();
            if (address == null)
                return null;

            return await AudioManager.Instance.PlaySFXAsync(address, volume, priority, ct);
        }

        /// <summary>
        /// 플레이리스트에서 SFX 재생 (3D)
        /// </summary>
        public async UniTask PlaySFXAtPositionAsync(Vector3 position, float volume = 1f, CancellationToken ct = default)
        {
            string address = GetNext();
            if (address == null)
                return;

            await AudioManager.Instance.PlaySFXAtPositionAsync(address, position, volume, ct);
        }

        // ========== 내부 로직 ==========

        private string GetNext()
        {
            if (addresses.Count == 0)
            {
                GameLogger.LogWarning("AudioPlaylist is empty");
                return null;
            }

            switch (playMode)
            {
                case PlayMode.Sequential:
                    string seqAddress = addresses[currentIndex];
                    currentIndex = (currentIndex + 1) % addresses.Count;
                    return seqAddress;

                case PlayMode.Random:
                    return addresses[Random.Range(0, addresses.Count)];

                case PlayMode.Shuffle:
                    if (currentIndex >= shuffleIndices.Count)
                    {
                        ShufflePlaylist();
                        currentIndex = 0;
                    }
                    string shuffleAddress = addresses[shuffleIndices[currentIndex]];
                    currentIndex++;
                    return shuffleAddress;

                default:
                    return addresses[0];
            }
        }

        private string GetPrevious()
        {
            if (addresses.Count == 0)
            {
                GameLogger.LogWarning("AudioPlaylist is empty");
                return null;
            }

            currentIndex = (currentIndex - 1 + addresses.Count) % addresses.Count;
            return addresses[currentIndex];
        }

        private void ShufflePlaylist()
        {
            shuffleIndices = new List<int>();
            for (int i = 0; i < addresses.Count; i++)
            {
                shuffleIndices.Add(i);
            }

            // Fisher-Yates 셔플
            for (int i = shuffleIndices.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffleIndices[i], shuffleIndices[j]) = (shuffleIndices[j], shuffleIndices[i]);
            }
        }

        // ========== 유틸리티 ==========

        /// <summary>
        /// 플레이 모드 변경
        /// </summary>
        public void SetPlayMode(PlayMode mode)
        {
            playMode = mode;

            if (mode == PlayMode.Shuffle)
            {
                ShufflePlaylist();
                currentIndex = 0;
            }
        }

        /// <summary>
        /// 플레이리스트에 주소 추가
        /// </summary>
        public void AddAddress(string address)
        {
            addresses.Add(address);

            if (playMode == PlayMode.Shuffle)
            {
                ShufflePlaylist();
            }
        }

        /// <summary>
        /// 플레이리스트에서 주소 제거
        /// </summary>
        public void RemoveAddress(string address)
        {
            addresses.Remove(address);

            if (playMode == PlayMode.Shuffle)
            {
                ShufflePlaylist();
            }
        }

        /// <summary>
        /// 플레이리스트 클리어
        /// </summary>
        public void Clear()
        {
            addresses.Clear();
            shuffleIndices?.Clear();
            currentIndex = 0;
        }

        /// <summary>
        /// 플레이리스트 크기
        /// </summary>
        public int Count => addresses.Count;
    }
}
