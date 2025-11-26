using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Utilities.GameLogger;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Core.Utilities;

namespace Core.Addressable.Tracker
{
    /// <summary>
    /// Addressable 에셋의 참조 카운팅을 관리합니다.
    /// 에셋 핸들을 저장하고 참조 카운트에 따라 실제 해제를 결정합니다.
    /// </summary>
    internal class AssetReferenceTracker
    {
        // 참조 카운터 (Address → AsyncOperationHandle)
        private readonly ReferenceCounter<string, AsyncOperationHandle> referenceCounter;

        // 에셋 타입 정보 (Address → Type)
        private readonly Dictionary<string, Type> assetTypes = new Dictionary<string, Type>();

        /// <summary>
        /// AssetReferenceTracker 생성자
        /// </summary>
        public AssetReferenceTracker()
        {
            // 참조 카운트가 0이 되면 Addressables.Release 호출
            referenceCounter = new ReferenceCounter<string, AsyncOperationHandle>(
                onReleaseCallback: (address, handle) =>
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                        Log($"[AssetReferenceTracker] 리소스 실제 해제: {address}");
                    }
                    assetTypes.Remove(address);
                },
                logName: "AssetReferenceTracker"
            );
        }

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

            referenceCounter.Add(address, handle);
            assetTypes[address] = assetType;
        }

        /// <summary>
        /// 기존 에셋의 참조 카운트를 증가시킵니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <returns>참조 증가 성공 여부</returns>
        public bool IncreaseReference(string address)
        {
            return referenceCounter.Increase(address);
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

            referenceCounter.Decrease(address);
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
            if (referenceCounter.TryGetValue(address, out handle))
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return true;
                }
                else
                {
                    // 유효하지 않은 핸들 제거
                    referenceCounter.Remove(address);
                    assetTypes.Remove(address);
                }
            }

            handle = default;
            return false;
        }

        /// <summary>
        /// 현재 로드된 모든 에셋 정보를 반환합니다.
        /// </summary>
        /// <returns>로드된 에셋 정보 리스트</returns>
        public IReadOnlyList<LoadedAssetInfo> GetLoadedAssets()
        {
            var counts = referenceCounter.GetAllCounts();
            var assetInfoList = new List<LoadedAssetInfo>(counts.Count);

            foreach (var kvp in counts)
            {
                string address = kvp.Key;
                int refCount = kvp.Value;

                // 타입 정보 조회
                Type assetType = assetTypes.ContainsKey(address) ? assetTypes[address] : null;

                assetInfoList.Add(new LoadedAssetInfo
                {
                    Address = address,
                    ReferenceCount = refCount,
                    AssetType = assetType
                });
            }

            return assetInfoList;
        }

        /// <summary>
        /// 현재 로드된 에셋 개수를 반환합니다.
        /// </summary>
        public int GetLoadedCount()
        {
            return referenceCounter.GetTotalCount();
        }

        #endregion

        #region 정리

        /// <summary>
        /// 모든 에셋 핸들을 강제로 해제합니다.
        /// </summary>
        /// <returns>해제된 에셋 개수</returns>
        public int ReleaseAll()
        {
            int count = referenceCounter.GetTotalCount();
            referenceCounter.Clear();
            assetTypes.Clear();

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
