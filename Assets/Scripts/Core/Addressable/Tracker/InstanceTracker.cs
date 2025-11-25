using System.Collections.Generic;
using UnityEngine;

using static Core.Addressable.AddressableLogger;
namespace Core.Addressable.Tracker
{
    /// <summary>
    /// 인스턴스화된 GameObject를 추적하여 메모리 관리를 지원합니다.
    /// GameObject와 Address를 매핑하여 안전한 해제를 보장합니다.
    /// </summary>
    internal class InstanceTracker
    {
        #region 필드

        // 인스턴스화된 오브젝트 추적 (GameObject → Address)
        private readonly Dictionary<GameObject, string> instantiatedObjects = new Dictionary<GameObject, string>();

        // GC Allocation 최소화를 위한 캐시된 리스트
        private readonly List<GameObject> cachedInstanceList = new List<GameObject>();
        private readonly List<GameObject> cachedAddressInstanceList = new List<GameObject>();

        #endregion

        #region 추적 관리

        /// <summary>
        /// 인스턴스 추적을 시작합니다.
        /// </summary>
        /// <param name="instance">추적할 GameObject</param>
        /// <param name="address">Addressable Address</param>
        public void TrackInstance(GameObject instance, string address)
        {
            if (instance == null)
            {
                LogWarning("[InstanceTracker] null 인스턴스 추적 시도");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                LogWarning("[InstanceTracker] Address가 비어있습니다.");
                return;
            }

            instantiatedObjects[instance] = address;
            Log($"[InstanceTracker] 인스턴스 추적 시작: {instance.name} (Address: {address})");
        }

        /// <summary>
        /// 인스턴스 추적을 해제합니다.
        /// </summary>
        /// <param name="instance">추적 해제할 GameObject</param>
        /// <returns>추적 해제 성공 여부</returns>
        public bool UntrackInstance(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (instantiatedObjects.Remove(instance))
            {
                Log($"[InstanceTracker] 인스턴스 추적 해제: {instance.name}");
                return true;
            }

            return false;
        }

        #endregion

        #region 조회

        /// <summary>
        /// GameObject에 대응하는 Address를 조회합니다.
        /// </summary>
        /// <param name="instance">GameObject</param>
        /// <param name="address">조회된 Address</param>
        /// <returns>Address가 존재하면 true</returns>
        public bool TryGetAddress(GameObject instance, out string address)
        {
            address = null;

            if (instance == null)
            {
                return false;
            }

            return instantiatedObjects.TryGetValue(instance, out address);
        }

        /// <summary>
        /// 모든 추적 중인 인스턴스를 반환합니다.
        /// (캐시된 리스트를 재사용하여 GC Allocation 최소화)
        /// </summary>
        /// <returns>인스턴스 리스트</returns>
        public IReadOnlyList<GameObject> GetAllInstances()
        {
            cachedInstanceList.Clear();
            cachedInstanceList.AddRange(instantiatedObjects.Keys);
            return cachedInstanceList;
        }

        /// <summary>
        /// 현재 추적 중인 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetInstanceCount()
        {
            return instantiatedObjects.Count;
        }

        /// <summary>
        /// 특정 Address로 생성된 인스턴스들을 조회합니다.
        /// (캐시된 리스트를 재사용하여 GC Allocation 최소화)
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <returns>해당 Address로 생성된 인스턴스 리스트</returns>
        public IReadOnlyList<GameObject> GetInstancesByAddress(string address)
        {
            cachedAddressInstanceList.Clear();
            foreach (var pair in instantiatedObjects)
            {
                if (pair.Value == address)
                {
                    cachedAddressInstanceList.Add(pair.Key);
                }
            }
            return cachedAddressInstanceList;
        }

        #endregion

        #region 정리

        /// <summary>
        /// 모든 인스턴스 추적을 해제합니다.
        /// (실제 GameObject 파괴는 수행하지 않습니다)
        /// </summary>
        public void Clear()
        {
            int count = instantiatedObjects.Count;
            instantiatedObjects.Clear();

            Log($"[InstanceTracker] 모든 인스턴스 추적 해제 완료 (개수: {count})");
        }

        #endregion
    }
}
