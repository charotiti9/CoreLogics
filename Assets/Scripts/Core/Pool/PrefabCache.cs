using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Utilities.GameLogger;

namespace Core.Pool
{
    /// <summary>
    /// 프리팹 로딩, 캐싱, 참조 카운팅을 담당하는 클래스
    /// </summary>
    /// <typeparam name="T">Component 타입</typeparam>
    public class PrefabCache<T> where T : Component
    {
        // 프리팹 캐시 (Address → 프리팹)
        private readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        // Address별 참조 카운트
        private readonly Dictionary<string, int> referenceCounts = new Dictionary<string, int>();

        // 프리팹 로더 (외부에서 주입)
        private readonly Func<string, CancellationToken, UniTask<GameObject>> loader;

        // 프리팹 해제 콜백 (외부에서 주입, 옵션)
        private readonly Action<string> releaser;

        // 풀 이름 (로깅용)
        private readonly string poolName;

        /// <summary>
        /// PrefabCache 생성자
        /// </summary>
        /// <param name="loader">프리팹 로드 함수</param>
        /// <param name="releaser">프리팹 해제 콜백 (옵션)</param>
        /// <param name="poolName">풀 이름 (로깅용)</param>
        public PrefabCache(
            Func<string, CancellationToken, UniTask<GameObject>> loader,
            Action<string> releaser,
            string poolName)
        {
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.releaser = releaser;
            this.poolName = poolName;
        }

        /// <summary>
        /// 프리팹을 로드합니다. 캐시가 있으면 캐시를 반환합니다.
        /// </summary>
        /// <param name="address">리소스 Address</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>로드된 프리팹</returns>
        public async UniTask<GameObject> LoadAsync(string address, CancellationToken ct)
        {
            // 캐시 확인
            if (cache.TryGetValue(address, out GameObject cachedPrefab))
            {
                Log($"[{poolName}] 캐시된 프리팹 사용: {address}");
                return cachedPrefab;
            }

            // 프리팹 로더를 통해 로드 (최초 1회만)
            GameObject prefab = await loader(address, ct);

            if (prefab == null)
            {
                LogError($"[{poolName}] 로드 실패: {address}");
                return null;
            }

            // 프리팹 캐시에 저장
            cache[address] = prefab;
            referenceCounts[address] = 0;

            Log($"[{poolName}] 프리팹 로드 및 캐시: {address}");
            return prefab;
        }

        /// <summary>
        /// 캐시된 프리팹을 조회합니다.
        /// </summary>
        /// <param name="address">리소스 Address</param>
        /// <param name="prefab">조회된 프리팹</param>
        /// <returns>캐시에 있으면 true</returns>
        public bool TryGet(string address, out GameObject prefab)
        {
            return cache.TryGetValue(address, out prefab);
        }

        /// <summary>
        /// Address의 참조 카운트를 증가시킵니다.
        /// </summary>
        /// <param name="address">리소스 Address</param>
        public void AddReference(string address)
        {
            if (!referenceCounts.ContainsKey(address))
            {
                referenceCounts[address] = 0;
            }

            referenceCounts[address]++;
        }

        /// <summary>
        /// Address의 참조 카운트를 감소시킵니다.
        /// 참조 카운트가 0이 되면 프리팹 캐시를 해제합니다.
        /// </summary>
        /// <param name="address">리소스 Address</param>
        public void RemoveReference(string address)
        {
            if (!referenceCounts.TryGetValue(address, out int count))
            {
                return;
            }

            referenceCounts[address] = count - 1;

            // 해당 Address의 모든 인스턴스가 제거되면 프리팹 캐시도 해제
            if (referenceCounts[address] <= 0)
            {
                referenceCounts.Remove(address);
                cache.Remove(address);

                // 프리팹 해제 콜백 호출 (있으면)
                releaser?.Invoke(address);

                Log($"[{poolName}] 프리팹 캐시 해제: {address}");
            }
        }

        /// <summary>
        /// 모든 캐시를 정리합니다.
        /// </summary>
        public void Clear()
        {
            // 모든 Address에 대해 해제 콜백 호출
            if (releaser != null)
            {
                foreach (var address in cache.Keys)
                {
                    releaser(address);
                }
            }

            cache.Clear();
            referenceCounts.Clear();

            Log($"[{poolName}] 프리팹 캐시 전체 정리 완료");
        }

        /// <summary>
        /// 현재 캐시된 프리팹 개수를 반환합니다.
        /// </summary>
        public int GetCachedCount()
        {
            return cache.Count;
        }
    }
}
