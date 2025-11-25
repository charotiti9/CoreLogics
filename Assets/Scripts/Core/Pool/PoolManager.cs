using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Utilities.GameLogger;

namespace Core.Pool
{
    /// <summary>
    /// 전역 풀링 시스템 관리자
    /// Attribute 기반으로 타입별 ObjectPool을 자동 생성/관리합니다.
    /// </summary>
    public static class PoolManager
    {
        // 타입별 ObjectPool 캐싱 (박싱 제거: ObjectPoolBase 사용)
        private static Dictionary<Type, ObjectPoolBase> pools = new Dictionary<Type, ObjectPoolBase>();

        // Parent Transform 캐싱 (이름 → Transform)
        private static Dictionary<string, Transform> parentCache = new Dictionary<string, Transform>();

        // 초기화 여부
        private static bool isInitialized = false;

        /// <summary>
        /// 타입별 Attribute 캐싱 (Reflection 비용 절감)
        /// 제네릭 정적 클래스는 타입별로 한 번만 초기화되므로 캐싱에 최적화
        /// </summary>
        private static class AttributeCache<T> where T : Component
        {
            public static readonly string Address;
            public static readonly string ParentName;

            static AttributeCache()
            {
                Type type = typeof(T);
                var attribute = type.GetCustomAttribute<PoolAddressAttribute>();

                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] PoolAddressAttribute가 정의되지 않았습니다. " +
                        $"클래스에 [PoolAddress(\"경로\", \"부모이름\")] Attribute를 추가하세요.");
                }

                if (string.IsNullOrEmpty(attribute.Address))
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] PoolAddressAttribute의 Address가 비어있습니다.");
                }

                Address = attribute.Address;
                ParentName = attribute.ParentName;
            }
        }

        /// <summary>
        /// PoolManager 초기화 (내부 전용)
        /// Get/Preload 메서드에서 자동으로 호출됩니다.
        /// </summary>
        private static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            pools = new Dictionary<Type, ObjectPoolBase>();
            parentCache = new Dictionary<string, Transform>();
            isInitialized = true;

            // 씬 전환 시 parent 캐시 자동 정리
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

            Log("[PoolManager] 자동 초기화 완료");
        }

        /// <summary>
        /// 씬이 언로드될 때 호출되는 콜백
        /// Parent 캐시를 정리하여 메모리 누수를 방지합니다.
        /// </summary>
        private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            if (parentCache.Count > 0)
            {
                Log($"[PoolManager] 씬 언로드 감지: {scene.name}, Parent 캐시 정리");
                parentCache.Clear();
            }
        }

        /// <summary>
        /// 풀에서 인스턴스를 가져오거나 새로 로드합니다.
        /// Attribute에서 address와 parentName을 자동으로 추출합니다.
        /// </summary>
        /// <typeparam name="T">가져올 Component 타입</typeparam>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public static async UniTask<T> Get<T>(CancellationToken ct) where T : Component
        {
            Initialize();

            // 1. Attribute 추출 (캐시됨)
            string address = AttributeCache<T>.Address;
            string parentName = AttributeCache<T>.ParentName;

            // 2. Parent Transform 검색 또는 생성
            Transform parent = GetOrCreateParent(parentName);

            // 3. ObjectPool 가져오기 또는 생성
            ObjectPool<Component> pool = GetOrCreatePool<T>();

            // 4. 인스턴스 가져오기
            return await pool.GetAsync<T>(address, parent, ct);
        }

        /// <summary>
        /// 인스턴스를 풀로 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환할 Component 타입</typeparam>
        /// <param name="instance">반환할 인스턴스</param>
        public static void Return<T>(T instance) where T : Component
        {
            if (!isInitialized)
            {
                LogWarning("[PoolManager] 초기화되지 않았습니다. 인스턴스를 파괴합니다.");
                if (instance != null)
                {
                    GameObject.Destroy(instance.gameObject);
                }
                return;
            }

            if (instance == null)
            {
                LogWarning("[PoolManager] null 인스턴스를 반환하려고 했습니다.");
                return;
            }

            Type type = typeof(T);

            if (!pools.TryGetValue(type, out ObjectPoolBase pool))
            {
                LogWarning($"[PoolManager] [{type.Name}] Pool이 존재하지 않습니다. 인스턴스를 파괴합니다.");
                GameObject.Destroy(instance.gameObject);
                return;
            }

            // 박싱 없이 직접 호출
            pool.Return(instance);
        }

        /// <summary>
        /// 특정 타입의 인스턴스를 미리 로드하여 풀에 채웁니다.
        /// </summary>
        /// <typeparam name="T">프리로드할 Component 타입</typeparam>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask Preload<T>(int count, CancellationToken ct) where T : Component
        {
            Initialize();

            // 1. Attribute 추출 (캐시됨)
            string address = AttributeCache<T>.Address;

            // 2. ObjectPool 가져오기 또는 생성
            ObjectPool<Component> pool = GetOrCreatePool<T>();

            // 3. 프리로드
            await pool.PreloadAsync<T>(address, count, ct);
        }

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// </summary>
        /// <typeparam name="T">비울 Component 타입</typeparam>
        public static void ClearType<T>() where T : Component
        {
            if (!isInitialized)
            {
                return;
            }

            Type type = typeof(T);

            if (!pools.TryGetValue(type, out ObjectPoolBase poolBase))
            {
                return;
            }

            // 타입 안전한 호출을 위해 캐스팅
            if (poolBase is ObjectPool<Component> pool)
            {
                pool.ClearType<T>();
            }

            Log($"[PoolManager] 타입 풀 비움: {type.Name}");
        }

        /// <summary>
        /// 모든 풀을 비웁니다.
        /// </summary>
        public static void Clear()
        {
            if (!isInitialized)
            {
                return;
            }

            foreach (var kvp in pools)
            {
                ObjectPoolBase pool = kvp.Value;

                // Reflection 없이 직접 호출
                pool.Clear();
            }

            pools.Clear();
            parentCache.Clear();

            // 씬 이벤트 구독 해제
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            isInitialized = false;

            Log("[PoolManager] 모든 풀 비움 및 정리 완료");
        }

        /// <summary>
        /// 현재 풀 상태를 출력합니다.
        /// 디버깅 용도로 사용합니다.
        /// </summary>
        public static void PrintDebugInfo()
        {
            Log("=== [PoolManager] 상태 ===");
            Log($"초기화 여부: {isInitialized}");
            Log($"Pool 개수: {pools.Count}");
            Log($"Parent 캐시: {parentCache.Count}");

            if (pools.Count > 0)
            {
                Log("\n[타입별 Pool]");
                foreach (var kvp in pools)
                {
                    Type type = kvp.Key;
                    Log($"- {type.Name}");
                }
            }

            if (parentCache.Count > 0)
            {
                Log("\n[Parent 캐시]");
                foreach (var kvp in parentCache)
                {
                    string name = kvp.Key;
                    Transform parent = kvp.Value;
                    string parentName = parent != null ? parent.name : "null";
                    Log($"- '{name}' → {parentName}");
                }
            }

            Log("===========================");
        }

        /// <summary>
        /// 특정 타입의 ObjectPool을 가져오거나 생성합니다.
        /// </summary>
        /// <typeparam name="T">Component 타입</typeparam>
        /// <returns>ObjectPool 인스턴스</returns>
        private static ObjectPool<Component> GetOrCreatePool<T>() where T : Component
        {
            Type type = typeof(T);

            // 캐시 확인 (박싱 없음)
            if (pools.TryGetValue(type, out ObjectPoolBase poolBase))
            {
                return poolBase as ObjectPool<Component>;
            }

            // 새로 생성
            ObjectPool<Component> newPool = ObjectPool<Component>.CreateForAddressable();
            pools[type] = newPool;

            Log($"[PoolManager] ObjectPool 생성: {type.Name}");

            return newPool;
        }

        /// <summary>
        /// ParentName으로 부모 Transform을 검색하거나 생성합니다.
        /// 캐싱을 통해 반복 검색을 방지합니다.
        /// </summary>
        /// <param name="parentName">부모 GameObject의 이름</param>
        /// <returns>부모 Transform (parentName이 없으면 null)</returns>
        private static Transform GetOrCreateParent(string parentName)
        {
            // parentName이 비어있으면 null 반환 (ObjectPool이 Pool Container 사용)
            if (string.IsNullOrEmpty(parentName))
            {
                return null;
            }

            // 캐시 확인
            if (parentCache.TryGetValue(parentName, out Transform cachedParent))
            {
                // 유효성 검사 (GameObject가 파괴되었을 수 있음)
                if (cachedParent != null)
                {
                    return cachedParent;
                }

                // 파괴되었으면 캐시 제거
                parentCache.Remove(parentName);
            }

            // 이름으로 GameObject 검색 (씬 내 모든 GameObject 검색)
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            GameObject parentObj = null;

            foreach (GameObject obj in allObjects)
            {
                if (obj.name == parentName)
                {
                    parentObj = obj;
                    break;
                }
            }

            // 없으면 새로 생성
            if (parentObj == null)
            {
                parentObj = new GameObject($"[Pool] {parentName}");
                Log($"[PoolManager] Parent 생성: {parentObj.name}");
            }
            else
            {
                Log($"[PoolManager] Parent 발견: {parentObj.name}");
            }

            Transform parent = parentObj.transform;

            // 캐싱
            parentCache[parentName] = parent;

            return parent;
        }
    }
}
