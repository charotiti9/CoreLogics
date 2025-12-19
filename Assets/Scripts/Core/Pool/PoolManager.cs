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
        // 타입별 ObjectPool 캐싱
        private static Dictionary<Type, ObjectPoolBase> pools = new Dictionary<Type, ObjectPoolBase>();

        // Parent Transform 캐싱
        private static Dictionary<string, Transform> parentCache = new Dictionary<string, Transform>();

        // PoolParentContainer 캐싱
        private static PoolParentContainer containerCache;

        // 초기화 여부
        private static bool isInitialized = false;

        /// <summary>
        /// 타입별 Attribute 캐싱
        /// </summary>
        private static class AttributeCache<T> where T : Component
        {
            public static readonly string Address;
            public static readonly string ParentName;
            public static readonly bool UsePoolContainer;
            public static readonly bool DontDestroyOnLoad;

            static AttributeCache()
            {
                Type type = typeof(T);
                var attribute = type.GetCustomAttribute<PoolAddressAttribute>();

                // 1. Attribute 존재 여부
                if (attribute == null)
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] PoolAddressAttribute가 정의되지 않았습니다.\n" +
                        $"클래스에 [PoolAddress(\"경로\")] Attribute를 추가하세요.\n" +
                        $"예시:\n" +
                        $"  [PoolAddress(\"UI/MyUI\")]\n" +
                        $"  public class {type.Name} : UIBase {{ ... }}");
                }

                // 2. Address 유효성
                if (string.IsNullOrWhiteSpace(attribute.Address))
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] PoolAddressAttribute의 Address가 비어있습니다.");
                }

                // 3. 경로 형식 검증 (백슬래시 금지)
                if (attribute.Address.Contains("\\"))
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] Address는 '\\' 대신 '/'를 사용해야 합니다.\n" +
                        $"잘못된 주소: {attribute.Address}");
                }

                // 4. ParentName 검증
                if (!attribute.UsePoolContainer && string.IsNullOrWhiteSpace(attribute.ParentName))
                {
                    throw new InvalidOperationException(
                        $"[PoolManager] [{type.Name}] UsePoolContainer가 false인데 ParentName이 비어있습니다.\n" +
                        $"생성자를 다음 중 하나로 사용하세요:\n" +
                        $"  [PoolAddress(\"경로\")] - Pool Container 사용\n" +
                        $"  [PoolAddress(\"경로\", \"부모이름\")] - 사용자 지정 부모");
                }

                Address = attribute.Address;
                ParentName = attribute.ParentName;
                UsePoolContainer = attribute.UsePoolContainer;
                DontDestroyOnLoad = attribute.DontDestroyOnLoad;
            }
        }

        /// <summary>
        /// PoolManager 초기화
        /// Get/Preload 메서드에서 자동으로 호출
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
        /// Attribute에서 address와 부모 설정을 자동으로 추출합니다.
        /// </summary>
        /// <typeparam name="T">가져올 Component 타입</typeparam>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public static async UniTask<T> GetFromPool<T>(CancellationToken ct) where T : Component
        {
            Initialize();

            // 1. Attribute 추출 (캐시됨)
            string address = AttributeCache<T>.Address;
            bool usePoolContainer = AttributeCache<T>.UsePoolContainer;

            // 2. Parent 결정
            Transform parent;
            if (usePoolContainer)
            {
                // Pool Container 사용 (null로 전달하면 ObjectPool이 poolContainer 사용)
                parent = null;
            }
            else
            {
                // 사용자 지정 부모 사용
                string parentName = AttributeCache<T>.ParentName;
                parent = GetOrCreateParent(parentName);
            }

            // 3. ObjectPool 가져오기 또는 생성
            ObjectPool<Component> pool = GetOrCreatePool<T>();

            // 4. 인스턴스 가져오기
            return await pool.GetAsync<T>(address, parent, usePoolContainer, ct);
        }

        /// <summary>
        /// 인스턴스를 풀로 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환할 Component 타입</typeparam>
        /// <param name="instance">반환할 인스턴스</param>
        public static void ReturnToPool<T>(T instance) where T : Component
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
        /// GetFromPool과 동일한 부모 아래에 프리로드됩니다.
        /// </summary>
        /// <typeparam name="T">프리로드할 Component 타입</typeparam>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask PreloadPool<T>(int count, CancellationToken ct) where T : Component
        {
            Initialize();

            // 1. Attribute 추출 (캐시됨)
            string address = AttributeCache<T>.Address;
            bool usePoolContainer = AttributeCache<T>.UsePoolContainer;

            // 2. Parent 결정 (Get과 동일한 부모 사용)
            Transform parent;
            if (usePoolContainer)
            {
                // Pool Container 사용
                parent = null;
            }
            else
            {
                // 사용자 지정 부모 사용
                string parentName = AttributeCache<T>.ParentName;
                parent = GetOrCreateParent(parentName);
            }

            // 3. ObjectPool 가져오기 또는 생성
            ObjectPool<Component> pool = GetOrCreatePool<T>();

            // 4. 프리로드 (parent 전달)
            await pool.PreloadAsync<T>(address, count, parent, ct);
        }

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// </summary>
        /// <typeparam name="T">비울 Component 타입</typeparam>
        public static void ClearPoolType<T>() where T : Component
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
        public static void ClearAllPools()
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

            // 캐시 확인
            if (pools.TryGetValue(type, out ObjectPoolBase poolBase))
            {
                return poolBase as ObjectPool<Component>;
            }

            // DontDestroyOnLoad 옵션 가져오기
            bool dontDestroyOnLoad = AttributeCache<T>.DontDestroyOnLoad;

            // 새로 생성 (타입 이름을 poolName으로 전달)
            ObjectPool<Component> newPool = ObjectPool<Component>.CreateForAddressable(
                dontDestroyOnLoad: dontDestroyOnLoad,
                poolName: type.Name);
            pools[type] = newPool;

            Log($"[PoolManager] ObjectPool 생성: {type.Name} (DontDestroyOnLoad: {dontDestroyOnLoad})");

            return newPool;
        }

        /// <summary>
        /// ParentName으로 부모 Transform을 검색하거나 생성합니다.
        /// PoolParentContainer를 활용하여 FindObjectsByType을 방지합니다.
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

            // Container 찾기 (한 번만 검색)
            if (containerCache == null)
            {
                containerCache = GameObject.FindFirstObjectByType<PoolParentContainer>();

                if (containerCache == null)
                {
                    // Container 없으면 생성
                    GameObject containerObj = new GameObject("[PoolParentContainer]");
                    containerCache = containerObj.AddComponent<PoolParentContainer>();
                    Log("[PoolManager] PoolParentContainer가 씬에 없어 자동 생성되었습니다.");
                }
            }

            // Container에서 Parent 찾기
            Transform parent = containerCache.FindParent(parentName);

            // 없으면 Container 아래에 생성
            if (parent == null)
            {
                parent = containerCache.CreateParent(parentName);
                Log($"[PoolManager] Parent 생성: {parent.name}");
            }
            else
            {
                Log($"[PoolManager] Parent 발견: {parent.name}");
            }

            // 캐싱
            parentCache[parentName] = parent;
            return parent;
        }
    }
}
