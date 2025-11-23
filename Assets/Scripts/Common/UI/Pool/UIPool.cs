using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Common.UI
{
    /// <summary>
    /// UI 인스턴스 풀링 (Addressable 기반)
    /// UI 재사용으로 생성/파괴 비용을 최소화합니다.
    /// </summary>
    public class UIPool
    {
        // 타입별 풀 관리
        private readonly Dictionary<Type, Queue<UIBase>> pools = new Dictionary<Type, Queue<UIBase>>();

        // Addressable 핸들 관리 (메모리 릭 방지)
        private readonly Dictionary<Type, AsyncOperationHandle<GameObject>> handles = new Dictionary<Type, AsyncOperationHandle<GameObject>>();

        // 풀 크기 제한 (타입당 최대 개수)
        private const int MAX_POOL_SIZE = 10;

        /// <summary>
        /// 풀에서 UI를 가져오거나 새로 생성합니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="addressableName">Addressable Address (프리팹 이름)</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>UI 인스턴스</returns>
        public async UniTask<T> GetAsync<T>(string addressableName, Transform parent, CancellationToken ct) where T : UIBase
        {
            Type type = typeof(T);

            // 풀에서 가져오기 시도
            if (pools.TryGetValue(type, out Queue<UIBase> pool) && pool.Count > 0)
            {
                UIBase ui = pool.Dequeue();
                ui.transform.SetParent(parent, false);
                return ui as T;
            }

            // 풀에 없으면 새로 로드
            return await LoadAsync<T>(addressableName, parent, ct);
        }

        /// <summary>
        /// Addressable에서 UI를 로드합니다.
        /// Addressable Address(프리팹 이름)로 자동으로 찾아줍니다.
        /// </summary>
        private async UniTask<T> LoadAsync<T>(string addressableName, Transform parent, CancellationToken ct) where T : UIBase
        {
            try
            {
                // Addressable 로드 (Address 이름으로)
                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(addressableName);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load UI prefab: {addressableName}");
                    Debug.LogError("프리팹이 Addressable Groups에 추가되어 있는지 확인하세요.");
                    Debug.LogError($"Addressable Address가 '{addressableName}'로 설정되어 있는지 확인하세요.");
                    return null;
                }

                // 인스턴스화
                GameObject prefab = handle.Result;
                GameObject instance = GameObject.Instantiate(prefab, parent);
                T ui = instance.GetComponent<T>();

                if (ui == null)
                {
                    Debug.LogError($"UI component not found: {typeof(T).Name}");
                    GameObject.Destroy(instance);
                    return null;
                }

                // 핸들 저장 (메모리 관리용)
                Type type = typeof(T);

                // 기존 핸들이 있으면 먼저 해제 (메모리 누수 방지)
                if (handles.TryGetValue(type, out AsyncOperationHandle<GameObject> existingHandle))
                {
                    if (existingHandle.IsValid())
                    {
                        Addressables.Release(existingHandle);
                    }
                }

                handles[type] = handle;

                Debug.Log($"UI 로드 성공: {addressableName}");
                return ui;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading UI {addressableName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// UI를 풀로 반환합니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="instance">반환할 UI 인스턴스</param>
        public void Return<T>(T instance) where T : UIBase
        {
            if (instance == null)
            {
                return;
            }

            // 제네릭 타입 대신 실제 런타임 타입 사용 (타입 안전성 향상)
            Type actualType = instance.GetType();

            // 풀 크기 제한 확인
            if (!pools.TryGetValue(actualType, out Queue<UIBase> pool))
            {
                pool = new Queue<UIBase>();
                pools[actualType] = pool;
            }

            // 풀 크기 초과 시 파괴
            if (pool.Count >= MAX_POOL_SIZE)
            {
                GameObject.Destroy(instance.gameObject);
                Debug.Log($"[UIPool] 풀 크기 초과로 {actualType.Name} 인스턴스 파괴 (최대: {MAX_POOL_SIZE})");
                return;
            }

            // 비활성화 후 풀에 추가
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        public void ClearType<T>() where T : UIBase
        {
            Type type = typeof(T);

            // 풀의 모든 인스턴스 파괴
            if (pools.TryGetValue(type, out Queue<UIBase> pool))
            {
                while (pool.Count > 0)
                {
                    UIBase ui = pool.Dequeue();
                    if (ui != null)
                    {
                        GameObject.Destroy(ui.gameObject);
                    }
                }
                pools.Remove(type);
            }

            // Addressable 핸들 해제
            if (handles.TryGetValue(type, out AsyncOperationHandle<GameObject> handle))
            {
                Addressables.Release(handle);
                handles.Remove(type);
            }
        }

        /// <summary>
        /// 모든 풀을 비웁니다.
        /// </summary>
        public void Clear()
        {
            // 모든 인스턴스 파괴
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    UIBase ui = pool.Dequeue();
                    if (ui != null)
                    {
                        GameObject.Destroy(ui.gameObject);
                    }
                }
            }
            pools.Clear();

            // 모든 Addressable 핸들 해제
            foreach (var handle in handles.Values)
            {
                Addressables.Release(handle);
            }
            handles.Clear();
        }
    }
}
