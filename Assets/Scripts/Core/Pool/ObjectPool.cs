using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Utilities.GameLogger;

namespace Core.Pool
{
    /// <summary>
    /// Component 기반 제네릭 풀링 시스템
    /// 프리팹 로더를 주입받아 Addressable, Resources 등 다양한 방식 지원
    /// 메모리 안전한 참조 카운팅 및 자동 정리 기능을 제공합니다.
    /// </summary>
    /// <typeparam name="T">풀링할 Component 타입</typeparam>
    public class ObjectPool<T> where T : Component
    {
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

        /// <summary>
        /// 제네릭 타입 캐싱 (typeof() 호출 비용 절감)
        /// 제네릭 정적 클래스는 타입별로 한 번만 생성되므로 캐싱에 최적화
        /// </summary>
        private static class TypeCache<TComponent> where TComponent : T
        {
            public static readonly Type Type = typeof(TComponent);
        }

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

        // 프리팹 캐시 (Address → 프리팹) - 중복 로드 방지
        private readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        // Address별 참조 카운트 (풀이 관리하는 프리팹의 참조)
        private readonly Dictionary<string, int> addressReferenceCounts = new Dictionary<string, int>();

        // 프리팹 로더 (외부에서 주입)
        private readonly Func<string, CancellationToken, UniTask<GameObject>> prefabLoader;

        // 프리팹 해제 콜백 (외부에서 주입, 옵션)
        private readonly Action<string> prefabReleaser;

        #region 생성자 및 Factory 메서드

        /// <summary>
        /// Private 생성자
        /// </summary>
        private ObjectPool(
            Func<string, CancellationToken, UniTask<GameObject>> prefabLoader,
            Action<string> prefabReleaser,
            int defaultMaxSize)
        {
            this.prefabLoader = prefabLoader ?? throw new ArgumentNullException(nameof(prefabLoader));
            this.prefabReleaser = prefabReleaser;
            this.poolName = $"ObjectPool<{typeof(T).Name}>";
            this.defaultMaxPoolSize = defaultMaxSize;

            // Pool Container 생성 (DontDestroyOnLoad)
            GameObject container = new GameObject($"[Pool] {this.poolName}");
            GameObject.DontDestroyOnLoad(container);
            poolContainer = container.transform;

            Log($"[{this.poolName}] 풀 생성됨 (기본 최대 크기: {defaultMaxSize})");
        }

        /// <summary>
        /// Addressable 방식의 ObjectPool 생성
        /// 프리팹 로드/해제를 AddressableManager로 자동 처리합니다.
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <returns>Addressable 전용 ObjectPool</returns>
        public static ObjectPool<T> CreateForAddressable(int defaultMaxSize = PoolConfig.DEFAULT_MAX_POOL_SIZE)
        {
            return new ObjectPool<T>(
                prefabLoader: (address, ct) => Core.Addressable.AddressableManager.Instance.LoadAssetAsync<GameObject>(address, ct),
                prefabReleaser: (address) => Core.Addressable.AddressableManager.Instance.Release(address),
                defaultMaxSize: defaultMaxSize
            );
        }

        /// <summary>
        /// Resources 방식의 ObjectPool 생성
        /// 프리팹 로드를 Resources.Load로 처리하며, 해제는 자동입니다.
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <returns>Resources 전용 ObjectPool</returns>
        public static ObjectPool<T> CreateForResources(int defaultMaxSize = PoolConfig.DEFAULT_MAX_POOL_SIZE)
        {
            return new ObjectPool<T>(
                prefabLoader: (address, ct) => UniTask.FromResult(Resources.Load<GameObject>(address)),
                prefabReleaser: null, // Resources는 명시적 해제 불필요
                defaultMaxSize: defaultMaxSize
            );
        }

        /// <summary>
        /// 커스텀 로더를 사용하는 ObjectPool 생성
        /// 고급 사용자를 위한 메서드입니다.
        /// </summary>
        /// <param name="prefabLoader">프리팹 로드 함수</param>
        /// <param name="prefabReleaser">프리팹 해제 콜백 (옵션)</param>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <returns>커스텀 ObjectPool</returns>
        public static ObjectPool<T> CreateCustom(
            Func<string, CancellationToken, UniTask<GameObject>> prefabLoader,
            Action<string> prefabReleaser = null,
            int defaultMaxSize = PoolConfig.DEFAULT_MAX_POOL_SIZE)
        {
            return new ObjectPool<T>(prefabLoader, prefabReleaser, defaultMaxSize);
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
            Type type = TypeCache<TComponent>.Type;
            maxPoolSizes[type] = maxSize;

            Log($"[{poolName}] {type.Name} 풀 크기 설정: {maxSize}");
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
        /// 풀에 있으면 재사용, 없으면 새로 로드합니다.
        /// </summary>
        /// <typeparam name="TComponent">가져올 Component 타입</typeparam>
        /// <param name="address">리소스 Address</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public async UniTask<TComponent> GetAsync<TComponent>(string address, Transform parent, CancellationToken ct) where TComponent : T
        {
            // 타입 캐싱 사용 (typeof() 호출 비용 절감)
            Type type = TypeCache<TComponent>.Type;

            // 1. 풀에서 재사용 시도
            if (pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var pooledInstance = pool.Dequeue();
                pooledInstance.Component.transform.SetParent(parent, false);
                pooledInstance.Component.gameObject.SetActive(true);

                // IPoolable 인터페이스 지원: OnGetFromPool 호출
                if (pooledInstance.Component is IPoolable poolable)
                {
                    poolable.OnGetFromPool();
                }

                Log($"[{poolName}] 재사용: {type.Name} (풀 남음: {pool.Count})");
                return pooledInstance.Component as TComponent;
            }

            // 2. 새로 로드
            return await LoadNewAsync<TComponent>(address, parent, ct);
        }

        /// <summary>
        /// 새로운 인스턴스를 로드합니다.
        /// 프리팹은 캐시되어 중복 로드를 방지합니다.
        /// </summary>
        private async UniTask<TComponent> LoadNewAsync<TComponent>(string address, Transform parent, CancellationToken ct) where TComponent : T
        {
            try
            {
                // 프리팹 캐시 확인
                GameObject prefab;

                if (prefabCache.TryGetValue(address, out prefab))
                {
                    // 캐시된 프리팹 사용
                    Log($"[{poolName}] 캐시된 프리팹 사용: {address}");
                }
                else
                {
                    // 프리팹 로더를 통해 로드 (최초 1회만)
                    prefab = await prefabLoader(address, ct);

                    if (prefab == null)
                    {
                        LogError($"[{poolName}] 로드 실패: {address}");
                        return null;
                    }

                    // 프리팹 캐시에 저장
                    prefabCache[address] = prefab;
                    addressReferenceCounts[address] = 0;

                    Log($"[{poolName}] 프리팹 로드 및 캐시: {address}");
                }

                // 인스턴스 생성
                GameObject instance = GameObject.Instantiate(prefab, parent);
                TComponent component = instance.GetComponent<TComponent>();

                if (component == null)
                {
                    Type cachedType = TypeCache<TComponent>.Type;
                    LogError($"[{poolName}] Component 없음: {cachedType.Name} (GameObject: {instance.name})");
                    GameObject.Destroy(instance);
                    return null;
                }

                // Address 추적 (메모리 관리용)
                instanceToAddress[component] = address;

                // Address별 참조 카운트 증가 (이 풀에서 생성한 인스턴스 추적)
                addressReferenceCounts[address]++;

                // IPoolable 인터페이스 지원: OnGetFromPool 호출
                if (component is IPoolable poolable)
                {
                    poolable.OnGetFromPool();
                }

                Type type = TypeCache<TComponent>.Type;
                Log($"[{poolName}] 새로 생성: {type.Name} (Address: {address}, 활성 인스턴스: {addressReferenceCounts[address]})");
                return component;
            }
            catch (OperationCanceledException)
            {
                Log($"[{poolName}] 로드 취소됨: {address}");
                throw;
            }
            catch (Exception ex)
            {
                LogError($"[{poolName}] 로드 중 예외 발생: {address}\n{ex.Message}");
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
                LogWarning($"[{poolName}] null 인스턴스 반환 시도");
                return;
            }

            // GameObject 파괴 여부 체크
            if (instance.gameObject == null)
            {
                LogWarning($"[{poolName}] 이미 파괴된 GameObject 반환 시도");
                instanceToAddress.Remove(instance);
                return;
            }

            Type type = instance.GetType();

            // Address 확인 (추적되지 않은 인스턴스 처리)
            if (!instanceToAddress.TryGetValue(instance, out var address))
            {
                LogWarning($"[{poolName}] 추적되지 않은 인스턴스 반환 시도: {type.Name}");
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
                Log($"[{poolName}] 풀 크기 초과로 파괴: {type.Name} (최대: {maxSize})");
                return;
            }

            // IPoolable 인터페이스 지원: OnReturnToPool 호출
            if (instance is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            // 풀로 반환 (비활성화 후 Container 아래로 이동)
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(poolContainer, false);

            pool.Enqueue(new PooledInstance(instance, address));

            Log($"[{poolName}] 반환: {type.Name} (풀: {pool.Count}/{maxSize})");
        }

        /// <summary>
        /// 인스턴스를 파괴하고 메모리를 정리합니다.
        /// </summary>
        private void DestroyInstance(T instance, string address)
        {
            instanceToAddress.Remove(instance);
            GameObject.Destroy(instance.gameObject);

            // Address별 참조 카운트 감소
            if (addressReferenceCounts.TryGetValue(address, out int count))
            {
                addressReferenceCounts[address] = count - 1;

                // 해당 Address의 모든 인스턴스가 제거되면 프리팹 캐시도 해제
                if (addressReferenceCounts[address] <= 0)
                {
                    addressReferenceCounts.Remove(address);
                    prefabCache.Remove(address);

                    // 프리팹 해제 콜백 호출 (있으면)
                    prefabReleaser?.Invoke(address);

                    Log($"[{poolName}] 프리팹 캐시 해제: {address}");
                }
            }
        }

        #endregion

        #region Preload (프리로드)

        /// <summary>
        /// 특정 타입의 인스턴스를 미리 로드하여 풀에 채웁니다.
        /// 게임 시작 시 필요한 오브젝트를 미리 생성하여 런타임 성능을 향상시킵니다.
        /// 프리팹은 한 번만 로드되고, 인스턴스만 count 개수만큼 생성됩니다.
        /// </summary>
        /// <typeparam name="TComponent">프리로드할 Component 타입</typeparam>
        /// <param name="address">리소스 Address</param>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadAsync<TComponent>(string address, int count, CancellationToken ct) where TComponent : T
        {
            if (count <= 0)
            {
                LogWarning($"[{poolName}] 프리로드 개수가 0 이하입니다: {count}");
                return;
            }

            Type type = TypeCache<TComponent>.Type;
            Log($"[{poolName}] 프리로드 시작: {type.Name} x{count}");

            // 프리팹은 한 번만 로드 (캐시 확인)
            GameObject prefab;
            if (prefabCache.TryGetValue(address, out prefab))
            {
                // 이미 캐시된 프리팹 사용
                Log($"[{poolName}] 캐시된 프리팹으로 프리로드: {address}");
            }
            else
            {
                // 프리팹 로더를 통해 로드 (최초 1회만)
                prefab = await prefabLoader(address, ct);

                if (prefab == null)
                {
                    LogError($"[{poolName}] 프리로드 실패: {address}");
                    return;
                }

                // 프리팹 캐시에 저장
                prefabCache[address] = prefab;
                addressReferenceCounts[address] = 0;

                Log($"[{poolName}] 프리팹 로드 및 캐시: {address}");
            }

            // 인스턴스만 count 개수만큼 생성하여 풀에 추가
            for (int i = 0; i < count; i++)
            {
                GameObject instance = GameObject.Instantiate(prefab, poolContainer);
                TComponent component = instance.GetComponent<TComponent>();

                if (component == null)
                {
                    LogError($"[{poolName}] Component 없음: {type.Name} (GameObject: {instance.name})");
                    GameObject.Destroy(instance);
                    continue;
                }

                // 바로 풀로 반환 (비활성화 상태로 풀에 저장)
                instanceToAddress[component] = address;
                addressReferenceCounts[address]++;

                // IPoolable 인터페이스 지원: OnReturnToPool 호출
                if (component is IPoolable poolable)
                {
                    poolable.OnReturnToPool();
                }

                instance.SetActive(false);

                // 풀 가져오기 또는 생성
                if (!pools.TryGetValue(type, out var pool))
                {
                    pool = new Queue<PooledInstance>();
                    pools[type] = pool;
                }

                pool.Enqueue(new PooledInstance(component, address));
            }

            Log($"[{poolName}] 프리로드 완료: {type.Name} x{count}");
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
            Type type = TypeCache<TComponent>.Type;

            if (pools.TryGetValue(type, out var pool))
            {
                int count = pool.Count;

                while (pool.Count > 0)
                {
                    var pooledInstance = pool.Dequeue();
                    DestroyInstance(pooledInstance.Component, pooledInstance.Address);
                }

                pools.Remove(type);

                Log($"[{poolName}] 타입 풀 비움: {type.Name} ({count}개 파괴)");
            }
        }

        /// <summary>
        /// 모든 타입의 풀을 비웁니다.
        /// 풀에 있는 모든 인스턴스와 활성 인스턴스를 파괴하고 Address를 해제합니다.
        /// </summary>
        public void Clear()
        {
            int pooledCount = 0;
            int activeCount = 0;

            // 1. 풀에 있는 인스턴스 파괴
            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var pooledInstance = pool.Dequeue();
                    DestroyInstance(pooledInstance.Component, pooledInstance.Address);
                    pooledCount++;
                }
            }

            pools.Clear();

            // 2. 활성 인스턴스도 파괴 (아직 Return 안 된 것들)
            var activeInstances = new List<T>(instanceToAddress.Keys);
            foreach (var instance in activeInstances)
            {
                if (instance != null && instanceToAddress.TryGetValue(instance, out var address))
                {
                    GameObject.Destroy(instance.gameObject);
                    instanceToAddress.Remove(instance);
                    activeCount++;

                    // Address 참조 카운트 감소 및 프리팹 해제
                    if (addressReferenceCounts.TryGetValue(address, out int count))
                    {
                        addressReferenceCounts[address] = count - 1;
                        if (addressReferenceCounts[address] <= 0)
                        {
                            addressReferenceCounts.Remove(address);
                            prefabCache.Remove(address);
                            prefabReleaser?.Invoke(address);
                        }
                    }
                }
            }

            Log($"[{poolName}] 전체 풀 비움: 풀링 {pooledCount}개, 활성 {activeCount}개 파괴");
        }

        #endregion

        #region 디버깅

        /// <summary>
        /// 현재 풀 상태를 반환합니다.
        /// </summary>
        public int GetPoolCount<TComponent>() where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;
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
            Log($"=== [{poolName}] 디버그 정보 ===");
            Log($"총 타입 개수: {pools.Count}");
            Log($"총 추적 인스턴스: {instanceToAddress.Count}");

            if (pools.Count > 0)
            {
                Log("\n[타입별 풀 상태]");
                foreach (var kvp in pools)
                {
                    Type type = kvp.Key;
                    int poolCount = kvp.Value.Count;
                    int maxSize = GetMaxPoolSize(type);

                    Log($"- {type.Name}: {poolCount}/{maxSize}");
                }
            }

            Log("=====================================");
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

            // 모든 Dictionary 정리
            instanceToAddress.Clear();
            prefabCache.Clear();
            addressReferenceCounts.Clear();
            maxPoolSizes.Clear();

            if (poolContainer != null)
            {
                GameObject.Destroy(poolContainer.gameObject);
                poolContainer = null;
            }

            Log($"[{poolName}] Dispose 완료");
        }

        #endregion
    }
}
