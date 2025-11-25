using System.Collections.Generic;
using UnityEngine;
using static Core.Utilities.GameLogger;
using Core.Addressable.Tracker;

namespace Core.Addressable.Debugging
{
    /// <summary>
    /// Addressable 시스템의 디버깅 정보를 수집하고 출력합니다.
    /// </summary>
    public class AddressableDebugger
    {

        private readonly AssetReferenceTracker referenceTracker;
        private readonly AssetLoadCache loadCache;
        private readonly InstanceTracker instanceTracker;

        /// <summary>
        /// AddressableDebugger 생성자
        /// </summary>
        /// <param name="referenceTracker">참조 추적기</param>
        /// <param name="loadCache">로드 캐시</param>
        /// <param name="instanceTracker">인스턴스 추적기</param>
        internal AddressableDebugger(
            AssetReferenceTracker referenceTracker,
            AssetLoadCache loadCache,
            InstanceTracker instanceTracker)
        {
            this.referenceTracker = referenceTracker;
            this.loadCache = loadCache;
            this.instanceTracker = instanceTracker;
        }

        #region 디버그 정보 조회

        /// <summary>
        /// 현재 로드된 모든 리소스 정보를 반환합니다.
        /// </summary>
        public IReadOnlyList<LoadedAssetInfo> GetLoadedAssets()
        {
            return referenceTracker.GetLoadedAssets();
        }

        /// <summary>
        /// 현재 로드된 리소스 개수를 반환합니다.
        /// </summary>
        public int GetLoadedCount()
        {
            return referenceTracker.GetLoadedCount();
        }

        /// <summary>
        /// 현재 추적 중인 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetInstanceCount()
        {
            return instanceTracker.GetInstanceCount();
        }

        /// <summary>
        /// 현재 로딩 중인 작업 개수를 반환합니다.
        /// </summary>
        public int GetLoadingCount()
        {
            return loadCache.GetLoadingCount();
        }

        /// <summary>
        /// 모든 추적 중인 인스턴스를 반환합니다.
        /// </summary>
        public IReadOnlyList<GameObject> GetAllInstances()
        {
            return instanceTracker.GetAllInstances();
        }

        #endregion

        #region 디버그 출력

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            Log("=== AddressableManager 디버그 정보 ===");
            Log($"로드된 리소스: {GetLoadedCount()}개");
            Log($"추적 중인 인스턴스: {GetInstanceCount()}개");
            Log($"로딩 중인 작업: {GetLoadingCount()}개");

            // 로드된 리소스 목록 출력
            var loadedAssets = GetLoadedAssets();
            if (loadedAssets.Count > 0)
            {
                Log("\n[로드된 리소스 목록]");
                foreach (var asset in loadedAssets)
                {
                    Log($"- {asset.Address} | Type: {asset.AssetType.Name} | RefCount: {asset.ReferenceCount}");
                }
            }

            // 추적 중인 인스턴스 목록 출력
            var instances = GetAllInstances();
            if (instances.Count > 0)
            {
                Log("\n[추적 중인 인스턴스 목록]");
                foreach (var instance in instances)
                {
                    if (instance != null && instanceTracker.TryGetAddress(instance, out var address))
                    {
                        Log($"- {instance.name} | Address: {address}");
                    }
                }
            }

            Log("=====================================");
        }

        #endregion
    }
}
