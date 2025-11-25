using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Utilities.GameLogger;

namespace Core.Pool
{
    /// <summary>
    /// 인스턴스 생명주기 관리 (생성/활성화/비활성화/파괴, IPoolable 콜백)
    /// </summary>
    /// <typeparam name="T">Component 타입</typeparam>
    public class InstanceLifecycle<T> where T : Component
    {
        // 인스턴스 → Address 매핑 (인스턴스 추적)
        private readonly Dictionary<T, string> tracking = new Dictionary<T, string>();

        // 풀 이름 (로깅용)
        private readonly string poolName;

        /// <summary>
        /// InstanceLifecycle 생성자
        /// </summary>
        /// <param name="poolName">풀 이름 (로깅용)</param>
        public InstanceLifecycle(string poolName)
        {
            this.poolName = poolName;
        }

        /// <summary>
        /// 인스턴스를 활성화하고 OnGetFromPool을 호출합니다.
        /// </summary>
        /// <param name="component">활성화할 Component</param>
        /// <param name="parent">부모 Transform</param>
        public void Activate(T component, Transform parent)
        {
            component.transform.SetParent(parent, false);
            component.gameObject.SetActive(true);

            // IPoolable 인터페이스 지원: OnGetFromPool 호출
            if (component is IPoolable poolable)
            {
                poolable.OnGetFromPool();
            }
        }

        /// <summary>
        /// 인스턴스를 비활성화하고 OnReturnToPool을 호출합니다.
        /// </summary>
        /// <param name="component">비활성화할 Component</param>
        /// <param name="poolContainer">풀 컨테이너 Transform</param>
        public void Deactivate(T component, Transform poolContainer)
        {
            // IPoolable 인터페이스 지원: OnReturnToPool 호출
            if (component is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            component.gameObject.SetActive(false);
            component.transform.SetParent(poolContainer, false);
        }

        /// <summary>
        /// 새로운 인스턴스를 생성하고 추적을 시작합니다.
        /// </summary>
        /// <typeparam name="TComponent">생성할 Component 타입</typeparam>
        /// <param name="prefab">프리팹</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="address">리소스 Address</param>
        /// <returns>생성된 Component</returns>
        public TComponent Create<TComponent>(GameObject prefab, Transform parent, string address) where TComponent : T
        {
            // 인스턴스 생성
            GameObject instance = GameObject.Instantiate(prefab, parent);
            TComponent component = instance.GetComponent<TComponent>();

            if (component == null)
            {
                LogError($"[{poolName}] Component 없음: {typeof(TComponent).Name} (GameObject: {instance.name})");
                GameObject.Destroy(instance);
                return null;
            }

            // 추적 시작
            TrackInstance(component, address);

            return component;
        }

        /// <summary>
        /// 인스턴스를 파괴합니다.
        /// </summary>
        /// <param name="component">파괴할 Component</param>
        public void Destroy(T component)
        {
            if (component != null && component.gameObject != null)
            {
                GameObject.Destroy(component.gameObject);
            }
        }

        /// <summary>
        /// 인스턴스 추적을 시작합니다.
        /// </summary>
        /// <param name="component">추적할 Component</param>
        /// <param name="address">리소스 Address</param>
        public void TrackInstance(T component, string address)
        {
            tracking[component] = address;
        }

        /// <summary>
        /// 인스턴스 추적을 해제합니다.
        /// </summary>
        /// <param name="component">추적 해제할 Component</param>
        public void UntrackInstance(T component)
        {
            tracking.Remove(component);
        }

        /// <summary>
        /// 인스턴스의 Address를 조회합니다.
        /// </summary>
        /// <param name="component">조회할 Component</param>
        /// <param name="address">조회된 Address</param>
        /// <returns>추적 중인 인스턴스면 true</returns>
        public bool TryGetAddress(T component, out string address)
        {
            return tracking.TryGetValue(component, out address);
        }

        /// <summary>
        /// 추적 중인 모든 인스턴스를 반환합니다 (Clear용).
        /// </summary>
        public List<(T component, string address)> GetAllTrackedInstances()
        {
            var result = new List<(T, string)>();

            foreach (var kvp in tracking)
            {
                result.Add((kvp.Key, kvp.Value));
            }

            return result;
        }

        /// <summary>
        /// 추적 정보를 정리합니다.
        /// </summary>
        public void Clear()
        {
            tracking.Clear();
        }

        /// <summary>
        /// 현재 추적 중인 활성 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetTrackedCount()
        {
            return tracking.Count;
        }
    }
}
