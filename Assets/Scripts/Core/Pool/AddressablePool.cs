using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Core.Addressable;

namespace Core.Pool
{
    /// <summary>
    /// Component 기반 제네릭 풀링 시스템
    /// Addressable 리소스를 풀링하여 생성/파괴 비용을 최소화합니다.
    /// 메모리 안전한 참조 카운팅 및 자동 정리 기능을 제공합니다.
    /// </summary>
    /// <typeparam name="T">풀링할 Component 타입</typeparam>
    public class AddressablePool<T> where T : Component
    {
        #region 내부 클래스

        /// <summary>
        /// 풀에 저장되는 인스턴스 정보
        /// Address를 함께 저장하여 메모리 안전성을 보장합니다.
        /// </summary>
        private class PooledInstance
        {
            public T Component;          // Component 참조
            public string Address;       // Address 추적 (메모리 관리용)

            public PooledInstance(T component, string address)
            {
                Component = component;
                Address = address;
            }
        }

        #endregion

        #region 필드

        // 타입별 풀 관리 (타입 → Queue)
        private readonly Dictionary<Type, Queue<PooledInstance>> pools = new Dictionary<Type, Queue<PooledInstance>>();

        // 인스턴스 → Address 매핑 (메모리 안전을 위한 완전한 추적)
        private readonly Dictionary<T, string> instanceToAddress = new Dictionary<T, string>();

        // 타입별 풀 크기 제한
        private readonly Dictionary<Type, int> maxPoolSizes = new Dictionary<Type, int>();
        private readonly int defaultMaxPoolSize;

        // Pool Container (Transform 관리)
        private Transform poolContainer;

        // 풀 이름
        private readonly string poolName;

        #endregion

        #region 생성자

        /// <summary>
        /// AddressablePool 생성자
        /// Pool Container를 자동으로 생성하여 DontDestroyOnLoad 처리합니다.
        /// </summary>
        /// <param name="poolName">풀 이름 (디버깅용)</param>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        public AddressablePool(string poolName = "AddressablePool", int defaultMaxSize = PoolConfig.DEFAULT_MAX_POOL_SIZE)
        {
            this.poolName = poolName;
            this.defaultMaxPoolSize = defaultMaxSize;

            // Pool Container 생성 (DontDestroyOnLoad)
            GameObject container = new GameObject($"[Pool] {poolName}");
            GameObject.DontDestroyOnLoad(container);
            poolContainer = container.transform;

            Debug.Log($"[{this.poolName}] 풀 생성됨 (기본 최대 크기: {defaultMaxSize})");
        }

        #endregion

        #region 풀 설정

        /// <summary>
        /// 특정 타입의 최대 풀 크기를 설정합니다.
        /// </summary>
        /// <typeparam name="TComponent">설정할 Component 타입</typeparam>
        /// <param name="maxSize">최대 풀 크기</param>
        public void SetPoolSize<TComponent>(int maxSize) where TComponent : T
        {
            Type type = typeof(TComponent);
            maxPoolSizes[type] = maxSize;

            Debug.Log($"[{poolName}] {type.Name} 풀 크기 설정: {maxSize}");
        }

        /// <summary>
        /// 특정 타입의 최대 풀 크기를 가져옵니다.
        /// </summary>
        private int GetMaxPoolSize(Type type)
        {
            return maxPoolSizes.TryGetValue(type, out int size) ? size : defaultMaxPoolSize;
        }

        #endregion

        #region Get (풀에서 가져오기)

        /// <summary>
        /// 풀에서 인스턴스를 가져오거나 새로 로드합니다.
        /// 풀에 있으면 재사용, 없으면 Addressable에서 새로 로드합니다.
        /// </summary>
        /// <typeparam name="TComponent">가져올 Component 타입</typeparam>
        /// <param name="address">Addressable Address</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public async UniTask<TComponent> GetAsync<TComponent>(string address, Transform parent, CancellationToken ct) where TComponent : T
        {
            Type type = typeof(TComponent);

            // 1. 풀에서 재사용 시도
            if (pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var pooledInstance = pool.Dequeue();
                pooledInstance.Component.transform.SetParent(parent, false);
                pooledInstance.Component.gameObject.SetActive(true);

                Debug.Log($"[{poolName}] 재사용: {type.Name} (풀 남음: {pool.Count})");
                return pooledInstance.Component as TComponent;
            }

            // 2. 새로 로드
            return await LoadNewAsync<TComponent>(address, parent, ct);
        }

        /// <summary>
        /// 새로운 인스턴스를 Addressable에서 로드합니다.
        /// </summary>
        private async UniTask<TComponent> LoadNewAsync<TComponent>(string address, Transform parent, CancellationToken ct) where TComponent : T
        {
            try
            {
                // AddressableManager로 프리팹 로드
                GameObject prefab = await AddressableManager.Instance.LoadAssetAsync<GameObject>(address, ct);

                if (prefab == null)
                {
                    Debug.LogError($"[{poolName}] 로드 실패: {address}");
                    return null;
                }

                // 인스턴스 생성
                GameObject instance = GameObject.Instantiate(prefab, parent);
                TComponent component = instance.GetComponent<TComponent>();

                if (component == null)
                {
                    Debug.LogError($"[{poolName}] Component 없음: {typeof(TComponent).Name} (GameObject: {instance.name})");
                    GameObject.Destroy(instance);

                    // 로드한 에셋 해제
                    AddressableManager.Instance.Release(address);
                    return null;
                }

                // Address 추적 (메모리 관리용)
                instanceToAddress[component] = address;

                Debug.Log($"[{poolName}] 새로 생성: {typeof(TComponent).Name} (Address: {address})");
                return component;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{poolName}] 로드 취소됨: {address}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{poolName}] 로드 중 예외 발생: {address}\n{ex.Message}");
                return null;
            }
        }

        #endregion

        #region Return (풀로 반환)

        /// <summary>
        /// 인스턴스를 풀로 반환합니다.
        /// 풀 크기 제한을 초과하면 인스턴스를 파괴하고 Address를 해제합니다.
        /// </summary>
        /// <param name="instance">반환할 Component 인스턴스</param>
        public void Return(T instance)
        {
            if (instance == null)
            {
                Debug.LogWarning($"[{poolName}] null 인스턴스 반환 시도");
                return;
            }

            Type type = instance.GetType();

            // Address 확인 (추적되지 않은 인스턴스 처리)
            if (!instanceToAddress.TryGetValue(instance, out var address))
            {
                Debug.LogWarning($"[{poolName}] 추적되지 않은 인스턴스 반환 시도: {type.Name}");
                GameObject.Destroy(instance.gameObject);
                return;
            }

            // 풀 가져오기 또는 생성
            if (!pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<PooledInstance>();
                pools[type] = pool;
            }

            // 풀 크기 제한 확인
            int maxSize = GetMaxPoolSize(type);

            if (pool.Count >= maxSize)
            {
                // 풀 크기 초과: 파괴 및 Address 해제
                DestroyInstance(instance, address);
                Debug.Log($"[{poolName}] 풀 크기 초과로 파괴: {type.Name} (최대: {maxSize})");
                return;
            }

            // 풀로 반환 (비활성화 후 Container 아래로 이동)
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(poolContainer, false);

            pool.Enqueue(new PooledInstance(instance, address));

            Debug.Log($"[{poolName}] 반환: {type.Name} (풀: {pool.Count}/{maxSize})");
        }

        /// <summary>
        /// 인스턴스를 파괴하고 메모리를 정리합니다.
        /// </summary>
        private void DestroyInstance(T instance, string address)
        {
            instanceToAddress.Remove(instance);
            GameObject.Destroy(instance.gameObject);
            AddressableManager.Instance.Release(address);
        }

        #endregion

        #region Preload (프리로드)

        /// <summary>
        /// 특정 타입의 인스턴스를 미리 로드하여 풀에 채웁니다.
        /// 게임 시작 시 필요한 오브젝트를 미리 생성하여 런타임 성능을 향상시킵니다.
        /// </summary>
        /// <typeparam name="TComponent">프리로드할 Component 타입</typeparam>
        /// <param name="address">Addressable Address</param>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadAsync<TComponent>(string address, int count, CancellationToken ct) where TComponent : T
        {
            if (count <= 0)
            {
                Debug.LogWarning($"[{poolName}] 프리로드 개수가 0 이하입니다: {count}");
                return;
            }

            Debug.Log($"[{poolName}] 프리로드 시작: {typeof(TComponent).Name} x{count}");

            for (int i = 0; i < count; i++)
            {
                var instance = await GetAsync<TComponent>(address, poolContainer, ct);
                if (instance != null)
                {
                    Return(instance);
                }
            }

            Debug.Log($"[{poolName}] 프리로드 완료: {typeof(TComponent).Name} x{count}");
        }

        #endregion

        #region Clear (정리)

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// 풀에 있는 모든 인스턴스를 파괴하고 Address를 해제합니다.
        /// </summary>
        /// <typeparam name="TComponent">비울 Component 타입</typeparam>
        public void ClearType<TComponent>() where TComponent : T
        {
            Type type = typeof(TComponent);

            if (pools.TryGetValue(type, out var pool))
            {
                int count = pool.Count;

                while (pool.Count > 0)
                {
                    var pooledInstance = pool.Dequeue();
                    DestroyInstance(pooledInstance.Component, pooledInstance.Address);
                }

                pools.Remove(type);

                Debug.Log($"[{poolName}] 타입 풀 비움: {type.Name} ({count}개 파괴)");
            }
        }

        /// <summary>
        /// 모든 타입의 풀을 비웁니다.
        /// 풀에 있는 모든 인스턴스를 파괴하고 Address를 해제합니다.
        /// </summary>
        public void Clear()
        {
            int totalCount = 0;

            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var pooledInstance = pool.Dequeue();
                    DestroyInstance(pooledInstance.Component, pooledInstance.Address);
                    totalCount++;
                }
            }

            pools.Clear();

            Debug.Log($"[{poolName}] 전체 풀 비움: {totalCount}개 파괴");
        }

        #endregion

        #region 디버깅

        /// <summary>
        /// 현재 풀 상태를 반환합니다.
        /// </summary>
        public int GetPoolCount<TComponent>() where TComponent : T
        {
            Type type = typeof(TComponent);
            return pools.TryGetValue(type, out var pool) ? pool.Count : 0;
        }

        /// <summary>
        /// 현재 추적 중인 활성 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetActiveCount()
        {
            return instanceToAddress.Count;
        }

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            Debug.Log($"=== [{poolName}] 디버그 정보 ===");
            Debug.Log($"총 타입 개수: {pools.Count}");
            Debug.Log($"총 추적 인스턴스: {instanceToAddress.Count}");

            if (pools.Count > 0)
            {
                Debug.Log("\n[타입별 풀 상태]");
                foreach (var kvp in pools)
                {
                    Type type = kvp.Key;
                    int poolCount = kvp.Value.Count;
                    int maxSize = GetMaxPoolSize(type);

                    Debug.Log($"- {type.Name}: {poolCount}/{maxSize}");
                }
            }

            Debug.Log("=====================================");
        }

        #endregion

        #region Dispose (리소스 정리)

        /// <summary>
        /// 풀의 모든 리소스를 정리합니다.
        /// 모든 인스턴스를 파괴하고 Pool Container를 제거합니다.
        /// </summary>
        public void Dispose()
        {
            Clear();

            if (poolContainer != null)
            {
                GameObject.Destroy(poolContainer.gameObject);
                poolContainer = null;
            }

            Debug.Log($"[{poolName}] Dispose 완료");
        }

        #endregion
    }
}
