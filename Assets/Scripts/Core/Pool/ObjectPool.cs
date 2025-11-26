using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Utilities.GameLogger;
using Core.Utilities;

namespace Core.Pool
{
    /// <summary>
    /// Component 기반 제네릭 풀링 시스템
    /// AddressableManager를 통해 프리팹을 로드하고 인스턴스를 풀링합니다.
    /// 메모리 안전한 참조 카운팅 및 자동 정리 기능을 제공합니다.
    /// </summary>
    /// <typeparam name="T">풀링할 Component 타입</typeparam>
    public class ObjectPool<T> : ObjectPoolBase where T : Component
    {
        /// <summary>
        /// 타입당 기본 최대 풀 크기
        /// 풀에 이 개수 이상의 인스턴스가 쌓이면 추가 반환되는 인스턴스는 파괴됩니다.
        /// </summary>
        private const int DEFAULT_MAX_POOL_SIZE = 10;

        // 서브 클래스들
        private readonly ReferenceCounter<string, GameObject> prefabCounter;
        private readonly PoolStorage<T> storage;
        private readonly InstanceLifecycle<T> lifecycle;
        private readonly InstanceTracker<T> tracker;

        // 프리팹 로더
        private readonly Func<string, CancellationToken, UniTask<GameObject>> prefabLoader;

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
            int defaultMaxSize,
            bool dontDestroyOnLoad)
        {
            this.poolName = $"ObjectPool<{typeof(T).Name}>";
            this.prefabLoader = prefabLoader;

            // 참조 카운터 초기화 (프리팹별 인스턴스 개수 추적)
            this.prefabCounter = new ReferenceCounter<string, GameObject>(
                onReleaseCallback: (address, prefab) =>
                {
                    // 모든 인스턴스가 제거되면 프리팹 해제
                    prefabReleaser?.Invoke(address);
                    Log($"[{poolName}] 프리팹 해제: {address}");
                },
                logName: poolName
            );

            // 서브 클래스 초기화
            this.storage = new PoolStorage<T>(defaultMaxSize, poolName);
            this.lifecycle = new InstanceLifecycle<T>(poolName);
            this.tracker = new InstanceTracker<T>();

            // Pool Container 생성
            GameObject container = new GameObject($"[Pool] {this.poolName}");

            // DontDestroyOnLoad 옵션 적용
            if (dontDestroyOnLoad)
            {
                GameObject.DontDestroyOnLoad(container);
            }

            poolContainer = container.transform;

            Log($"[{this.poolName}] 풀 생성됨 (기본 최대 크기: {defaultMaxSize}, DontDestroyOnLoad: {dontDestroyOnLoad})");
        }

        /// <summary>
        /// Addressable 방식의 ObjectPool 생성
        /// 프리팹 로드/해제를 AddressableManager로 자동 처리합니다.
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <param name="dontDestroyOnLoad">씬 전환 시에도 유지할지 여부 (기본값: false)</param>
        /// <returns>Addressable 전용 ObjectPool</returns>
        public static ObjectPool<T> CreateForAddressable(int defaultMaxSize = DEFAULT_MAX_POOL_SIZE, bool dontDestroyOnLoad = false)
        {
            return new ObjectPool<T>(
                prefabLoader: (address, ct) => Core.Addressable.AddressableManager.Instance.LoadAssetAsync<GameObject>(address, ct),
                prefabReleaser: (address) => Core.Addressable.AddressableManager.Instance.Release(address),
                defaultMaxSize: defaultMaxSize,
                dontDestroyOnLoad: dontDestroyOnLoad
            );
        }

        /// <summary>
        /// Resources 방식의 ObjectPool 생성
        /// 프리팹 로드를 Resources.Load로 처리하며, 해제는 자동입니다.
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <param name="dontDestroyOnLoad">씬 전환 시에도 유지할지 여부 (기본값: false)</param>
        /// <returns>Resources 전용 ObjectPool</returns>
        public static ObjectPool<T> CreateForResources(int defaultMaxSize = DEFAULT_MAX_POOL_SIZE, bool dontDestroyOnLoad = false)
        {
            return new ObjectPool<T>(
                prefabLoader: (address, ct) => UniTask.FromResult(Resources.Load<GameObject>(address)),
                prefabReleaser: null, // Resources는 명시적 해제 불필요
                defaultMaxSize: defaultMaxSize,
                dontDestroyOnLoad: dontDestroyOnLoad
            );
        }

        /// <summary>
        /// 커스텀 로더를 사용하는 ObjectPool 생성
        /// 고급 사용자를 위한 메서드입니다.
        /// </summary>
        /// <param name="prefabLoader">프리팹 로드 함수</param>
        /// <param name="prefabReleaser">프리팹 해제 콜백 (옵션)</param>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <param name="dontDestroyOnLoad">씬 전환 시에도 유지할지 여부 (기본값: false)</param>
        /// <returns>커스텀 ObjectPool</returns>
        public static ObjectPool<T> CreateCustom(
            Func<string, CancellationToken, UniTask<GameObject>> prefabLoader,
            Action<string> prefabReleaser = null,
            int defaultMaxSize = DEFAULT_MAX_POOL_SIZE,
            bool dontDestroyOnLoad = false)
        {
            return new ObjectPool<T>(prefabLoader, prefabReleaser, defaultMaxSize, dontDestroyOnLoad);
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
        /// <param name="parent">부모 Transform (null이면 poolContainer 사용)</param>
        /// <param name="usePoolContainer">Pool Container 사용 여부</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public async UniTask<TComponent> GetAsync<TComponent>(string address, Transform parent, bool usePoolContainer, CancellationToken ct) where TComponent : T
        {
            // 1. 풀에서 재사용 시도
            if (storage.TryTake<TComponent>(out T component, out string pooledAddress))
            {
                // usePoolContainer가 true면 poolContainer 사용, 아니면 parent 사용
                Transform targetParent = usePoolContainer ? poolContainer : parent;
                lifecycle.Activate(component, targetParent);
                return component as TComponent;
            }

            // 2. 새로 로드
            return await LoadNewAsync<TComponent>(address, parent, usePoolContainer, ct);
        }

        /// <summary>
        /// 새로운 인스턴스를 로드합니다.
        /// 프리팹은 캐시되어 중복 로드를 방지합니다.
        /// </summary>
        private async UniTask<TComponent> LoadNewAsync<TComponent>(string address, Transform parent, bool usePoolContainer, CancellationToken ct) where TComponent : T
        {
            try
            {
                // 프리팹 로드 (캐시 확인 또는 새로 로드)
                GameObject prefab;

                if (!prefabCounter.TryGetValue(address, out prefab))
                {
                    // 최초 로드
                    prefab = await prefabLoader(address, ct);

                    if (prefab == null)
                    {
                        LogError($"[{poolName}] 프리팹 로드 실패: {address} (Prefab이 null입니다)");
                        return null;
                    }

                    // 프리팹 캐싱 (참조 카운트 1로 시작)
                    prefabCounter.Add(address, prefab);
                    Log($"[{poolName}] 프리팹 로드 및 캐싱: {address}");
                }

                // usePoolContainer가 true면 poolContainer 사용, 아니면 parent 사용
                Transform targetParent = usePoolContainer ? poolContainer : parent;

                // 인스턴스 생성 (targetParent로 생성)
                TComponent component = lifecycle.Create<TComponent>(prefab, targetParent);

                if (component == null)
                {
                    LogError($"[{poolName}] Component 생성 실패: {typeof(TComponent).Name} (Address: {address})");
                    return null;
                }

                // 추적 시작 (Type 정보 및 부모 정보 포함)
                Type componentType = TypeCache<TComponent>.Type;
                tracker.Track(component, address, componentType, targetParent);

                // 활성화 (targetParent 사용)
                lifecycle.Activate(component, targetParent);

                // 프리팹 참조 카운트 증가 (인스턴스 개수 추적)
                prefabCounter.Increase(address);

                Log($"[{poolName}] 새로 생성: {componentType.Name} (Address: {address})");
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
        /// 인스턴스를 풀로 반환합니다 (타입 안전한 버전).
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
                tracker.Untrack(instance);
                return;
            }

            // 추적 정보 가져오기 (Address, Type, TargetParent 모두)
            if (!tracker.TryGetInfo(instance, out var address, out Type componentType, out Transform targetParent))
            {
                LogWarning($"[{poolName}] 추적되지 않은 인스턴스 반환 시도");
                lifecycle.Destroy(instance);
                return;
            }

            // 풀 크기 제한 확인 (Reflection 없이 Type으로 직접 호출)
            bool isFull = storage.IsFullByType(componentType);

            if (isFull)
            {
                // 풀 크기 초과: 파괴 및 Address 해제
                DestroyInstance(instance, address);
                Log($"[{poolName}] 풀 크기 초과로 파괴: {componentType.Name}");
                return;
            }

            // 비활성화 (원래 부모로 이동)
            lifecycle.Deactivate(instance, targetParent);

            // 풀에 저장 (Reflection 없이 Type으로 직접 호출)
            storage.StoreByType(instance, componentType, address);
        }

        /// <summary>
        /// 인스턴스를 풀로 반환합니다 (ObjectPoolBase override).
        /// Component를 받아 T로 캐스팅하여 처리합니다.
        /// </summary>
        /// <param name="instance">반환할 Component 인스턴스</param>
        public override void Return(Component instance)
        {
            if (instance is T typedInstance)
            {
                Return(typedInstance);
            }
            else
            {
                LogWarning($"[{poolName}] 타입 불일치: {instance.GetType().Name}은 {typeof(T).Name}이 아닙니다.");
            }
        }

        /// <summary>
        /// 인스턴스를 파괴하고 메모리를 정리합니다.
        /// </summary>
        private void DestroyInstance(T instance, string address)
        {
            tracker.Untrack(instance);
            lifecycle.Destroy(instance);
            prefabCounter.Decrease(address);
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
        /// <param name="parent">부모 Transform (null이면 poolContainer 사용)</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadAsync<TComponent>(string address, int count, Transform parent, CancellationToken ct) where TComponent : T
        {
            if (count <= 0)
            {
                LogWarning($"[{poolName}] 프리로드 개수가 0 이하입니다: {count}");
                return;
            }

            Type type = TypeCache<TComponent>.Type;
            Log($"[{poolName}] 프리로드 시작: {type.Name} x{count}");

            // 프리팹 로드 (캐시 확인 또는 새로 로드)
            GameObject prefab;

            if (!prefabCounter.TryGetValue(address, out prefab))
            {
                // 최초 로드
                prefab = await prefabLoader(address, ct);

                if (prefab == null)
                {
                    LogError($"[{poolName}] 프리로드 실패: {address}");
                    return;
                }

                // 프리팹 캐싱 (참조 카운트 1로 시작)
                prefabCounter.Add(address, prefab);
                Log($"[{poolName}] 프리팹 로드 및 캐싱: {address}");
            }

            // parent가 null이면 poolContainer 사용
            Transform targetParent = parent ?? poolContainer;

            // 인스턴스만 count 개수만큼 생성하여 풀에 추가
            for (int i = 0; i < count; i++)
            {
                // 인스턴스 생성 (targetParent 사용)
                TComponent component = lifecycle.Create<TComponent>(prefab, targetParent);

                if (component == null)
                {
                    continue;
                }

                // 추적 시작 (Type 정보 및 부모 정보 포함)
                Type componentType = TypeCache<TComponent>.Type;
                tracker.Track(component, address, componentType, targetParent);

                // 프리팹 참조 카운트 증가 (인스턴스 개수 추적)
                prefabCounter.Increase(address);

                // 비활성화 및 풀에 저장 (부모는 이미 설정됨)
                lifecycle.Deactivate(component, targetParent);
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
        /// 모든 타입의 풀을 비웁니다 (ObjectPoolBase override).
        /// 풀에 있는 모든 인스턴스와 활성 인스턴스를 파괴하고 Address를 해제합니다.
        /// </summary>
        public override void Clear()
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
            var activeInstances = tracker.GetAll();
            foreach (var (component, address) in activeInstances)
            {
                if (component != null)
                {
                    lifecycle.Destroy(component);
                    tracker.Untrack(component);
                    prefabCounter.Decrease(address);
                    activeCount++;
                }
            }

            tracker.Clear();
            prefabCounter.Clear();

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
            return tracker.Count;
        }

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            Log($"=== [{poolName}] 디버그 정보 ===");
            Log($"총 추적 인스턴스: {tracker.Count}");
            Log($"캐시된 프리팹: {prefabCounter.GetTotalCount()}");

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
        /// 풀의 모든 리소스를 정리합니다 (ObjectPoolBase override).
        /// 모든 인스턴스를 파괴하고 Pool Container를 제거합니다.
        /// </summary>
        public override void Dispose()
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
