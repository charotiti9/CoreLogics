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
        /// 새로운 인스턴스를 생성합니다.
        /// </summary>
        /// <typeparam name="TComponent">생성할 Component 타입</typeparam>
        /// <param name="prefab">프리팹</param>
        /// <param name="parent">부모 Transform</param>
        /// <returns>생성된 Component</returns>
        public TComponent Create<TComponent>(GameObject prefab, Transform parent) where TComponent : T
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
    }
}
