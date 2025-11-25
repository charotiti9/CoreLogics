using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Addressable.AddressableLogger;

namespace Core.Addressable.Tracker
{
    /// <summary>
    /// 중복 로드를 방지하기 위한 로딩 작업 캐시입니다.
    /// 동일한 Address를 동시에 로드하는 경우, 하나의 작업을 공유합니다.
    /// </summary>
    internal class AssetLoadCache
    {
        #region 필드

        // 로딩 중인 작업 캐시 (중복 로드 방지)
        private readonly Dictionary<string, UniTask<UnityEngine.Object>> loadingTasks = new Dictionary<string, UniTask<UnityEngine.Object>>();

        #endregion

        #region 캐시 관리

        /// <summary>
        /// 로딩 중인 작업을 조회합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="task">조회된 작업</param>
        /// <returns>로딩 중인 작업이 있으면 true</returns>
        public bool TryGetLoadingTask(string address, out UniTask<UnityEngine.Object> task)
        {
            if (loadingTasks.TryGetValue(address, out task))
            {
                Log($"[AssetLoadCache] 로딩 중인 작업 발견: {address}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 로딩 작업을 캐시에 등록합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="task">로딩 작업</param>
        public void RegisterLoadingTask(string address, UniTask<UnityEngine.Object> task)
        {
            loadingTasks[address] = task;
            Log($"[AssetLoadCache] 로딩 작업 등록: {address}");
        }

        /// <summary>
        /// 로딩 완료 후 캐시에서 제거합니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        public void CompleteLoadingTask(string address)
        {
            if (loadingTasks.Remove(address))
            {
                Log($"[AssetLoadCache] 로딩 완료 및 캐시 제거: {address}");
            }
        }

        #endregion

        #region 조회

        /// <summary>
        /// 현재 로딩 중인 작업 개수를 반환합니다.
        /// </summary>
        public int GetLoadingCount()
        {
            return loadingTasks.Count;
        }

        #endregion

        #region 정리

        /// <summary>
        /// 모든 캐시를 제거합니다.
        /// </summary>
        public void Clear()
        {
            int count = loadingTasks.Count;
            loadingTasks.Clear();

            Log($"[AssetLoadCache] 모든 캐시 제거 완료 (개수: {count})");
        }

        #endregion
    }
}
