using System;
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
        /// 제네릭 타입 캐싱 (typeof() 호출 비용 절감)
        /// 제네릭 정적 클래스는 타입별로 한 번만 생성되므로 캐싱에 최적화
        /// </summary>
        private static class TypeCache<TComponent> where TComponent : T
        {
            public static readonly Type Type = typeof(TComponent);
        }

        // 서브 클래스들
        private readonly PrefabCache<T> prefabCache;
        private readonly PoolStorage<T> storage;
        private readonly InstanceLifecycle<T> lifecycle;

        // Pool Container (Transform 관리)
        private Transform poolContainer;

        // 풀 이름
        private readonly string poolName;

        #region 생성자 및 Factory 메서드

        /// <summary>
        /// Private 생성자
        /// </summary>
        private ObjectPool(
            Func<string, CancellationToken, UniTask<GameObject>> prefabLoader,
            Action<string> prefabReleaser,
            int defaultMaxSize)
        {
            this.poolName = $"ObjectPool<{typeof(T).Name}>";

            // 서브 클래스 초기화
            this.prefabCache = new PrefabCache<T>(prefabLoader, prefabReleaser, poolName);
            this.storage = new PoolStorage<T>(defaultMaxSize, poolName);
            this.lifecycle = new InstanceLifecycle<T>(poolName);

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
            storage.SetMaxSize<TComponent>(maxSize);
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
            // 1. 풀에서 재사용 시도
            if (storage.TryTake<TComponent>(out T component, out string pooledAddress))
            {
                lifecycle.Activate(component, parent);
                return component as TComponent;
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
                // 프리팹 로드 (캐시 활용)
                GameObject prefab = await prefabCache.LoadAsync(address, ct);

                if (prefab == null)
                {
                    return null;
                }

                // 인스턴스 생성 및 추적
                TComponent component = lifecycle.Create<TComponent>(prefab, parent, address);

                if (component == null)
                {
                    return null;
                }

                // 활성화
                lifecycle.Activate(component, parent);

                // Address별 참조 카운트 증가
                prefabCache.AddReference(address);

                Type type = TypeCache<TComponent>.Type;
                Log($"[{poolName}] 새로 생성: {type.Name} (Address: {address})");
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
                lifecycle.UntrackInstance(instance);
                return;
            }

            Type type = instance.GetType();

            // Address 확인 (추적되지 않은 인스턴스 처리)
            if (!lifecycle.TryGetAddress(instance, out var address))
            {
                LogWarning($"[{poolName}] 추적되지 않은 인스턴스 반환 시도: {type.Name}");
                lifecycle.Destroy(instance);
                return;
            }

            // 풀 크기 제한 확인 (제네릭 메서드 호출을 위한 리플렉션 사용)
            bool isFull = IsPoolFull(type);

            if (isFull)
            {
                // 풀 크기 초과: 파괴 및 Address 해제
                DestroyInstance(instance, address);
                Log($"[{poolName}] 풀 크기 초과로 파괴: {type.Name}");
                return;
            }

            // 비활성화
            lifecycle.Deactivate(instance, poolContainer);

            // 풀에 저장 (제네릭 메서드 호출을 위한 리플렉션 사용)
            StoreToPool(instance, type, address);
        }

        /// <summary>
        /// 특정 타입의 풀이 꽉 찼는지 확인합니다 (리플렉션 사용).
        /// </summary>
        private bool IsPoolFull(Type type)
        {
            var method = typeof(PoolStorage<T>).GetMethod("IsFull").MakeGenericMethod(type);
            return (bool)method.Invoke(storage, null);
        }

        /// <summary>
        /// 풀에 저장합니다 (리플렉션 사용).
        /// </summary>
        private void StoreToPool(T instance, Type type, string address)
        {
            var method = typeof(PoolStorage<T>).GetMethod("Store").MakeGenericMethod(type);
            method.Invoke(storage, new object[] { instance, address });
        }

        /// <summary>
        /// 인스턴스를 파괴하고 메모리를 정리합니다.
        /// </summary>
        private void DestroyInstance(T instance, string address)
        {
            lifecycle.UntrackInstance(instance);
            lifecycle.Destroy(instance);
            prefabCache.RemoveReference(address);
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

            // 프리팹 로드 (캐시 활용)
            GameObject prefab = await prefabCache.LoadAsync(address, ct);

            if (prefab == null)
            {
                LogError($"[{poolName}] 프리로드 실패: {address}");
                return;
            }

            // 인스턴스만 count 개수만큼 생성하여 풀에 추가
            for (int i = 0; i < count; i++)
            {
                // 인스턴스 생성 및 추적
                TComponent component = lifecycle.Create<TComponent>(prefab, poolContainer, address);

                if (component == null)
                {
                    continue;
                }

                // Address별 참조 카운트 증가
                prefabCache.AddReference(address);

                // 비활성화 및 풀에 저장
                lifecycle.Deactivate(component, poolContainer);
                storage.Store<TComponent>(component, address);
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

            var items = storage.ClearType<TComponent>();

            foreach (var (component, address) in items)
            {
                DestroyInstance(component, address);
            }

            Log($"[{poolName}] 타입 풀 비움: {type.Name} ({items.Count}개 파괴)");
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
            var pooledItems = storage.GetAll();
            foreach (var (component, address) in pooledItems)
            {
                DestroyInstance(component, address);
                pooledCount++;
            }

            storage.Clear();

            // 2. 활성 인스턴스도 파괴 (아직 Return 안 된 것들)
            var activeInstances = lifecycle.GetAllTrackedInstances();
            foreach (var (component, address) in activeInstances)
            {
                if (component != null)
                {
                    lifecycle.Destroy(component);
                    lifecycle.UntrackInstance(component);
                    prefabCache.RemoveReference(address);
                    activeCount++;
                }
            }

            lifecycle.Clear();
            prefabCache.Clear();

            Log($"[{poolName}] 전체 풀 비움: 풀링 {pooledCount}개, 활성 {activeCount}개 파괴");
        }

        #endregion

        #region 디버깅

        /// <summary>
        /// 현재 풀 상태를 반환합니다.
        /// </summary>
        public int GetPoolCount<TComponent>() where TComponent : T
        {
            return storage.GetCount<TComponent>();
        }

        /// <summary>
        /// 현재 추적 중인 활성 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetActiveCount()
        {
            return lifecycle.GetTrackedCount();
        }

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            Log($"=== [{poolName}] 디버그 정보 ===");
            Log($"총 추적 인스턴스: {lifecycle.GetTrackedCount()}");
            Log($"캐시된 프리팹: {prefabCache.GetCachedCount()}");

            var storageInfo = storage.GetDebugInfo();
            if (storageInfo.Count > 0)
            {
                Log("\n[타입별 풀 상태]");
                foreach (var kvp in storageInfo)
                {
                    Type type = kvp.Key;
                    var (current, max) = kvp.Value;

                    Log($"- {type.Name}: {current}/{max}");
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
