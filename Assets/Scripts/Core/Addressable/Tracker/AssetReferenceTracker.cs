using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Addressable.AddressableLogger;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Addressable.Tracker
{
    /// <summary>
    /// Addressable 에셋의 참조 카운팅을 관리합니다.
    /// 에셋 핸들을 저장하고 참조 카운트에 따라 실제 해제를 결정합니다.
    /// </summary>
    internal class AssetReferenceTracker
    {
        #region 내부 클래스

        /// <summary>
        /// 에셋 핸들 정보 (참조 카운팅 포함)
        /// </summary>
        private class AssetHandle
        {
            public AsyncOperationHandle Handle;
            public int ReferenceCount;
            public string Address;
            public Type AssetType;

            public AssetHandle(AsyncOperationHandle handle, string address, Type assetType)
            {
                Handle = handle;
                ReferenceCount = 1;
                Address = address;
                AssetType = assetType;
            }
        }

        #endregion

        #region 필드

        // 로드된 핸들 추적 (참조 카운팅)
        private readonly Dictionary<string, AssetHandle> loadHandles = new Dictionary<string, AssetHandle>();

        // GC Allocation 최소화를 위한 캐시된 리스트
        private readonly List<LoadedAssetInfo> cachedAssetInfoList = new List<LoadedAssetInfo>();

        #endregion

        #region 참조 관리

        /// <summary>
        /// 새로운 에셋 핸들을 추가하고 참조를 시작합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="handle">AsyncOperationHandle</param>
        /// <param name="assetType">에셋 타입</param>
        public void AddReference(string address, AsyncOperationHandle handle, Type assetType)
        {
            if (string.IsNullOrEmpty(address))
            {
                LogWarning("[AssetReferenceTracker] Address가 비어있습니다.");
                return;
            }

            var assetHandle = new AssetHandle(handle, address, assetType);
            loadHandles[address] = assetHandle;

            Log($"[AssetReferenceTracker] 참조 추가: {address} (RefCount: 1)");
        }

        /// <summary>
        /// 기존 에셋의 참조 카운트를 증가시킵니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <returns>참조 증가 성공 여부</returns>
        public bool IncreaseReference(string address)
        {
            if (loadHandles.TryGetValue(address, out var assetHandle))
            {
                assetHandle.ReferenceCount++;
                Log($"[AssetReferenceTracker] 참조 증가: {address} (RefCount: {assetHandle.ReferenceCount})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 에셋의 참조 카운트를 감소시킵니다.
        /// 참조 카운트가 0이 되면 실제로 해제합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        public void DecreaseReference(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            if (loadHandles.TryGetValue(address, out var assetHandle))
            {
                assetHandle.ReferenceCount--;

                Log($"[AssetReferenceTracker] 참조 감소: {address} (RefCount: {assetHandle.ReferenceCount})");

                // 참조 카운트가 0이 되면 실제 해제
                if (assetHandle.ReferenceCount <= 0)
                {
                    if (assetHandle.Handle.IsValid())
                    {
                        Addressables.Release(assetHandle.Handle);
                        Log($"[AssetReferenceTracker] 리소스 실제 해제: {address}");
                    }
                    loadHandles.Remove(address);
                }
            }
            else
            {
                LogWarning($"[AssetReferenceTracker] 해제할 리소스를 찾을 수 없습니다: {address}");
            }
        }

        #endregion

        #region 조회

        /// <summary>
        /// 에셋 핸들을 조회합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="handle">조회된 핸들</param>
        /// <returns>핸들이 존재하고 유효하면 true</returns>
        public bool TryGetHandle(string address, out AsyncOperationHandle handle)
        {
            handle = default;

            if (loadHandles.TryGetValue(address, out var assetHandle))
            {
                if (assetHandle.Handle.IsValid() && assetHandle.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    handle = assetHandle.Handle;
                    return true;
                }
                else
                {
                    // 유효하지 않은 핸들 제거
                    loadHandles.Remove(address);
                }
            }

            return false;
        }

        /// <summary>
        /// 현재 로드된 모든 에셋 정보를 반환합니다.
        /// (캐시된 리스트를 재사용하여 GC Allocation 최소화)
        /// </summary>
        /// <returns>로드된 에셋 정보 리스트</returns>
        public IReadOnlyList<LoadedAssetInfo> GetLoadedAssets()
        {
            cachedAssetInfoList.Clear();
            foreach (var handle in loadHandles.Values)
            {
                cachedAssetInfoList.Add(new LoadedAssetInfo
                {
                    Address = handle.Address,
                    ReferenceCount = handle.ReferenceCount,
                    AssetType = handle.AssetType
                });
            }
            return cachedAssetInfoList;
        }

        /// <summary>
        /// 현재 로드된 에셋 개수를 반환합니다.
        /// </summary>
        public int GetLoadedCount()
        {
            return loadHandles.Count;
        }

        #endregion

        #region 정리

        /// <summary>
        /// 모든 에셋 핸들을 강제로 해제합니다.
        /// </summary>
        /// <returns>해제된 에셋 개수</returns>
        public int ReleaseAll()
        {
            int count = 0;

            foreach (var assetHandle in loadHandles.Values)
            {
                if (assetHandle.Handle.IsValid())
                {
                    Addressables.Release(assetHandle.Handle);
                    count++;
                }
            }

            loadHandles.Clear();

            Log($"[AssetReferenceTracker] 모든 리소스 강제 해제 완료 (개수: {count})");
            return count;
        }

        #endregion
    }

    /// <summary>
    /// 로드된 에셋 정보 (디버깅용)
    /// </summary>
    public class LoadedAssetInfo
    {
        public string Address { get; set; }
        public int ReferenceCount { get; set; }
        public Type AssetType { get; set; }
    }
}
