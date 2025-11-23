using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Addressable
{
    /// <summary>
    /// Addressable 리소스 로딩을 중앙에서 관리하는 싱글톤
    /// 참조 카운팅, 중복 로드 방지, 인스턴스 추적 등 고급 기능 제공
    /// </summary>
    public class AddressableManager : LazyMonoSingleton<AddressableManager>
    {
        #region 내부 클래스

        /// <summary>
        /// 에셋 핸들 정보 (참조 카운팅 포함)
        /// </summary>
        private class AssetHandle
        {
            public AsyncOperationHandle Handle;
            public int ReferenceCount;
            public string Address;
            public Type AssetType;

            public AssetHandle(AsyncOperationHandle handle, string address, Type assetType)
            {
                Handle = handle;
                ReferenceCount = 1;
                Address = address;
                AssetType = assetType;
            }
        }

        /// <summary>
        /// 로드된 에셋 정보 (디버깅용)
        /// </summary>
        public class LoadedAssetInfo
        {
            public string Address { get; set; }
            public int ReferenceCount { get; set; }
            public Type AssetType { get; set; }
        }

        #endregion

        #region 필드

        // 로드된 핸들 추적 (참조 카운팅)
        private readonly Dictionary<string, AssetHandle> loadHandles = new Dictionary<string, AssetHandle>();

        // 로딩 중인 작업 캐시 (중복 로드 방지)
        private readonly Dictionary<string, UniTask<UnityEngine.Object>> loadingTasks = new Dictionary<string, UniTask<UnityEngine.Object>>();

        // 인스턴스화된 오브젝트 추적
        private readonly Dictionary<GameObject, string> instantiatedObjects = new Dictionary<GameObject, string>();

        #endregion

        #region 기본 로드/해제

        /// <summary>
        /// Addressable 리소스를 비동기로 로드합니다.
        /// 참조 카운팅 및 중복 로드 방지 기능 포함
        /// </summary>
        /// <typeparam name="T">로드할 리소스 타입</typeparam>
        /// <param name="address">Addressable Address</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>로드된 리소스</returns>
        public async UniTask<T> LoadAssetAsync<T>(string address, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError("[AddressableManager] Address가 비어있습니다.");
                return null;
            }

            // 1. 이미 로드된 경우 참조 카운트 증가 후 반환
            if (loadHandles.TryGetValue(address, out var existingHandle))
            {
                if (existingHandle.Handle.IsValid() && existingHandle.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    existingHandle.ReferenceCount++;
                    Debug.Log($"[AddressableManager] 리소스 재사용: {address} (RefCount: {existingHandle.ReferenceCount})");
                    return existingHandle.Handle.Result as T;
                }
                else
                {
                    // 유효하지 않은 핸들 제거
                    loadHandles.Remove(address);
                }
            }

            // 2. 로딩 중인 작업이 있으면 대기 (중복 로드 방지)
            if (loadingTasks.TryGetValue(address, out var loadingTask))
            {
                Debug.Log($"[AddressableManager] 로딩 중인 작업 대기: {address}");
                var result = await loadingTask;
                return result as T;
            }

            // 3. 새로 로드
            var task = LoadAssetInternalAsync<T>(address, ct);
            loadingTasks[address] = task.ContinueWith(obj => obj as UnityEngine.Object);

            try
            {
                var asset = await task;
                return asset;
            }
            finally
            {
                // 로딩 완료 후 캐시에서 제거
                loadingTasks.Remove(address);
            }
        }

        /// <summary>
        /// 내부 로드 로직
        /// </summary>
        private async UniTask<T> LoadAssetInternalAsync<T>(string address, CancellationToken ct) where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AddressableManager] 리소스 로드 실패: {address}");
                    Debug.LogError("Addressable Groups에 추가되어 있는지 확인하세요.");
                    Debug.LogError($"Address가 '{address}'로 설정되어 있는지 확인하세요.");
                    return null;
                }

                // 핸들 저장 (참조 카운트 1로 시작)
                var assetHandle = new AssetHandle(handle, address, typeof(T));
                loadHandles[address] = assetHandle;

                Debug.Log($"[AddressableManager] 리소스 로드 성공: {address} (RefCount: 1)");
                return handle.Result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] 로드 중 예외 발생: {address}\n{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 라벨로 여러 Addressable 리소스를 일괄 로드합니다.
        /// </summary>
        /// <typeparam name="T">로드할 리소스 타입</typeparam>
        /// <param name="label">Addressable Label</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>로드된 리소스 리스트</returns>
        public async UniTask<IList<T>> LoadAssetsAsync<T>(string label, CancellationToken ct = default) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                Debug.LogError("[AddressableManager] Label이 비어있습니다.");
                return null;
            }

            // 이미 로드된 경우 참조 카운트 증가
            if (loadHandles.TryGetValue(label, out var existingHandle))
            {
                if (existingHandle.Handle.IsValid() && existingHandle.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    existingHandle.ReferenceCount++;
                    Debug.Log($"[AddressableManager] 라벨 리소스 재사용: {label} (RefCount: {existingHandle.ReferenceCount})");
                    return existingHandle.Handle.Result as IList<T>;
                }
                else
                {
                    loadHandles.Remove(label);
                }
            }

            try
            {
                var handle = Addressables.LoadAssetsAsync<T>(label, null);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AddressableManager] 라벨 리소스 로드 실패: {label}");
                    Debug.LogError($"Label '{label}'이 Addressable Groups에 설정되어 있는지 확인하세요.");
                    return null;
                }

                // 핸들 저장
                var assetHandle = new AssetHandle(handle, label, typeof(T));
                loadHandles[label] = assetHandle;

                Debug.Log($"[AddressableManager] 라벨 리소스 로드 성공: {label} (개수: {handle.Result.Count}, RefCount: 1)");
                return handle.Result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] 라벨 로드 중 예외 발생: {label}\n{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 특정 Address의 리소스 참조 카운트를 감소시킵니다.
        /// 참조 카운트가 0이 되면 실제 해제됩니다.
        /// </summary>
        /// <param name="address">해제할 Addressable Address</param>
        public void Release(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return;
            }

            if (loadHandles.TryGetValue(address, out var assetHandle))
            {
                assetHandle.ReferenceCount--;

                Debug.Log($"[AddressableManager] 참조 카운트 감소: {address} (RefCount: {assetHandle.ReferenceCount})");

                // 참조 카운트가 0이 되면 실제 해제
                if (assetHandle.ReferenceCount <= 0)
                {
                    if (assetHandle.Handle.IsValid())
                    {
                        Addressables.Release(assetHandle.Handle);
                        Debug.Log($"[AddressableManager] 리소스 실제 해제: {address}");
                    }
                    loadHandles.Remove(address);
                }
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] 해제할 리소스를 찾을 수 없습니다: {address}");
            }
        }

        /// <summary>
        /// 모든 리소스 핸들을 강제로 해제합니다.
        /// 씬 전환 시 호출하여 메모리를 정리합니다.
        /// </summary>
        public void ReleaseAll()
        {
            int count = 0;

            foreach (var assetHandle in loadHandles.Values)
            {
                if (assetHandle.Handle.IsValid())
                {
                    Addressables.Release(assetHandle.Handle);
                    count++;
                }
            }

            loadHandles.Clear();
            loadingTasks.Clear();

            Debug.Log($"[AddressableManager] 모든 리소스 강제 해제 완료 (개수: {count})");
        }

        #endregion

        #region 인스턴스화 및 추적

        /// <summary>
        /// 프리팹을 로드하고 인스턴스화합니다.
        /// 생성된 GameObject는 자동으로 추적됩니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>생성된 GameObject 인스턴스</returns>
        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null, CancellationToken ct = default)
        {
            GameObject prefab = await LoadAssetAsync<GameObject>(address, ct);

            if (prefab == null)
            {
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);

            // 인스턴스 추적
            instantiatedObjects[instance] = address;

            Debug.Log($"[AddressableManager] 인스턴스 생성 및 추적: {address}");
            return instance;
        }

        /// <summary>
        /// 인스턴스화된 GameObject를 해제합니다.
        /// GameObject를 파괴하고 리소스 참조 카운트를 감소시킵니다.
        /// </summary>
        /// <param name="instance">해제할 GameObject 인스턴스</param>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instantiatedObjects.TryGetValue(instance, out var address))
            {
                // GameObject 파괴
                UnityEngine.Object.Destroy(instance);

                // 추적에서 제거
                instantiatedObjects.Remove(instance);

                // 리소스 참조 카운트 감소
                Release(address);

                Debug.Log($"[AddressableManager] 인스턴스 해제: {address}");
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] 추적되지 않은 인스턴스입니다. 직접 파괴합니다.");
                UnityEngine.Object.Destroy(instance);
            }
        }

        /// <summary>
        /// 모든 추적 중인 인스턴스를 해제합니다.
        /// </summary>
        public void ReleaseAllInstances()
        {
            var instances = instantiatedObjects.Keys.ToList();

            foreach (var instance in instances)
            {
                ReleaseInstance(instance);
            }

            Debug.Log($"[AddressableManager] 모든 인스턴스 해제 완료 (개수: {instances.Count})");
        }

        #endregion

        #region 프리로드

        /// <summary>
        /// 여러 리소스를 미리 로드합니다.
        /// </summary>
        /// <param name="addresses">로드할 Address 목록</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadAsync(IEnumerable<string> addresses, CancellationToken ct = default)
        {
            var tasks = addresses.Select(address => LoadAssetAsync<UnityEngine.Object>(address, ct));
            await UniTask.WhenAll(tasks);

            Debug.Log($"[AddressableManager] 프리로드 완료 (개수: {addresses.Count()})");
        }

        /// <summary>
        /// 특정 라벨의 모든 리소스를 프리로드합니다.
        /// </summary>
        /// <param name="label">Addressable Label</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadByLabelAsync(string label, CancellationToken ct = default)
        {
            await LoadAssetsAsync<UnityEngine.Object>(label, ct);

            Debug.Log($"[AddressableManager] 라벨 프리로드 완료: {label}");
        }

        #endregion

        #region 디버깅

        /// <summary>
        /// 현재 로드된 모든 리소스 정보를 반환합니다.
        /// </summary>
        /// <returns>로드된 리소스 정보 리스트</returns>
        public IReadOnlyList<LoadedAssetInfo> GetLoadedAssets()
        {
            return loadHandles.Values.Select(handle => new LoadedAssetInfo
            {
                Address = handle.Address,
                ReferenceCount = handle.ReferenceCount,
                AssetType = handle.AssetType
            }).ToList();
        }

        /// <summary>
        /// 현재 로드된 리소스 개수를 반환합니다.
        /// </summary>
        public int GetLoadedCount()
        {
            return loadHandles.Count;
        }

        /// <summary>
        /// 현재 추적 중인 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetInstanceCount()
        {
            return instantiatedObjects.Count;
        }

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            Debug.Log("=== AddressableManager 디버그 정보 ===");
            Debug.Log($"로드된 리소스: {loadHandles.Count}개");
            Debug.Log($"추적 중인 인스턴스: {instantiatedObjects.Count}개");
            Debug.Log($"로딩 중인 작업: {loadingTasks.Count}개");

            if (loadHandles.Count > 0)
            {
                Debug.Log("\n[로드된 리소스 목록]");
                foreach (var handle in loadHandles.Values)
                {
                    Debug.Log($"- {handle.Address} | Type: {handle.AssetType.Name} | RefCount: {handle.ReferenceCount}");
                }
            }

            if (instantiatedObjects.Count > 0)
            {
                Debug.Log("\n[추적 중인 인스턴스 목록]");
                foreach (var pair in instantiatedObjects)
                {
                    Debug.Log($"- {pair.Key.name} | Address: {pair.Value}");
                }
            }

            Debug.Log("=====================================");
        }

        #endregion

        #region Unity 생명주기

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 씬 파괴 시 모든 리소스 해제
            ReleaseAllInstances();
            ReleaseAll();
        }

        #endregion
    }
}
