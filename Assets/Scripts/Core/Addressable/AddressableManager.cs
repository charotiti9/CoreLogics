using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Core.Addressable.AddressableLogger;
using UnityEngine.ResourceManagement.AsyncOperations;
using Core.Addressable.Tracker;
using Core.Addressable.Debugging;

namespace Core.Addressable
{
    /// <summary>
    /// Addressable 리소스 로딩을 중앙에서 관리하는 싱글톤
    /// 참조 카운팅, 중복 로드 방지, 인스턴스 추적 등 고급 기능 제공
    /// Facade 패턴을 사용하여 서브시스템을 조합합니다.
    /// </summary>
    public class AddressableManager : LazyMonoSingleton<AddressableManager>
    {
        // 서브시스템
        private AssetReferenceTracker referenceTracker;
        private AssetLoadCache loadCache;
        private InstanceTracker instanceTracker;
        private AddressableDebugger debugger;

        protected override void Awake()
        {
            base.Awake();

            // 서브시스템 초기화
            referenceTracker = new AssetReferenceTracker();
            loadCache = new AssetLoadCache();
            instanceTracker = new InstanceTracker();
            debugger = new AddressableDebugger(referenceTracker, loadCache, instanceTracker);

            Log("[AddressableManager] 초기화 완료");
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 씬 파괴 시 모든 리소스 해제
            ReleaseAllInstances();
            ReleaseAll();
        }

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
            Log($"[AddressableManager] 로드 시도: {address}");
            if (string.IsNullOrEmpty(address))
            {
                LogError("[AddressableManager] Address가 비어있습니다.");
                return null;
            }

            // 1. 이미 로드된 경우 참조 카운트 증가 후 반환
            if (referenceTracker.TryGetHandle(address, out var existingHandle))
            {
                referenceTracker.IncreaseReference(address);
                Log($"[AddressableManager] 리소스 재사용: {address}");
                return existingHandle.Result as T;
            }

            // 2. 로딩 중인 작업이 있으면 대기 (중복 로드 방지)
            if (loadCache.TryGetLoadingTask(address, out var loadingTask))
            {
                Log($"[AddressableManager] 로딩 중인 작업 대기: {address}");

                // 이미 로딩 중이면 완료될 때까지 대기
                var result = await loadingTask;

                // 로드 완료 후 참조 증가
                referenceTracker.IncreaseReference(address);
                Log($"[AddressableManager] 중복 로드 방지 후 참조 증가: {address}");
                return result as T;
            }

            // 3. 새로 로드 (Memoization 패턴)
            var taskCompletionSource = new UniTaskCompletionSource<UnityEngine.Object>();
            loadCache.RegisterLoadingTask(address, taskCompletionSource.Task);

            try
            {
                var asset = await LoadAssetInternalAsync<T>(address, ct);
                taskCompletionSource.TrySetResult(asset);
                return asset;
            }
            catch (Exception ex)
            {
                taskCompletionSource.TrySetException(ex);
                throw;
            }
            finally
            {
                // 로딩 완료 후 캐시에서 제거
                loadCache.CompleteLoadingTask(address);
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
                    LogError($"[AddressableManager] 리소스 로드 실패: {address}");
                    LogError("Addressable Groups에 추가되어 있는지 확인하세요.");
                    LogError($"Address가 '{address}'로 설정되어 있는지 확인하세요.");
                    return null;
                }

                // 핸들 저장 (참조 카운트 1로 시작)
                referenceTracker.AddReference(address, handle, typeof(T));

                Log($"[AddressableManager] 리소스 로드 성공: {address}");
                return handle.Result;
            }
            catch (Exception ex)
            {
                LogError($"[AddressableManager] 로드 중 예외 발생: {address}\n{ex.Message}");
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
                LogError("[AddressableManager] Label이 비어있습니다.");
                return null;
            }

            // 이미 로드된 경우 참조 카운트 증가
            if (referenceTracker.TryGetHandle(label, out var existingHandle))
            {
                referenceTracker.IncreaseReference(label);
                Log($"[AddressableManager] 라벨 리소스 재사용: {label}");
                return existingHandle.Result as IList<T>;
            }

            try
            {
                var handle = Addressables.LoadAssetsAsync<T>(label, null);
                await handle.ToUniTask(cancellationToken: ct);

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    LogError($"[AddressableManager] 라벨 리소스 로드 실패: {label}");
                    LogError($"Label '{label}'이 Addressable Groups에 설정되어 있는지 확인하세요.");
                    return null;
                }

                // 핸들 저장
                referenceTracker.AddReference(label, handle, typeof(T));

                Log($"[AddressableManager] 라벨 리소스 로드 성공: {label} (개수: {handle.Result.Count})");
                return handle.Result;
            }
            catch (Exception ex)
            {
                LogError($"[AddressableManager] 라벨 로드 중 예외 발생: {label}\n{ex.Message}");
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
            referenceTracker.DecreaseReference(address);
        }

        /// <summary>
        /// 모든 리소스 핸들을 강제로 해제합니다.
        /// 씬 전환 시 호출하여 메모리를 정리합니다.
        /// </summary>
        public void ReleaseAll()
        {
            int count = referenceTracker.ReleaseAll();
            loadCache.Clear();

            Log($"[AddressableManager] 모든 리소스 강제 해제 완료 (개수: {count})");
        }

        #endregion

        #region 인스턴스화 및 추적

        /// <summary>
        /// 프리팹을 로드하고 인스턴스화합니다.
        /// 생성된 GameObject는 자동으로 추적됩니다.
        /// 프리팹은 한 번만 로드되고, 인스턴스별로 참조 카운트가 관리되지 않습니다.
        /// </summary>
        /// <param name="address">Addressable Address</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>생성된 GameObject 인스턴스</returns>
        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null, CancellationToken ct = default)
        {
            // 프리팹은 한 번만 로드 (이미 로드된 경우 재사용)
            GameObject prefab = await LoadAssetAsync<GameObject>(address, ct);

            if (prefab == null)
            {
                LogError($"[AddressableManager] 프리팹 로드 실패로 인스턴스 생성 불가: {address}");
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent);

            // 인스턴스 추적
            instanceTracker.TrackInstance(instance, address);

            Log($"[AddressableManager] 인스턴스 생성 및 추적: {address}");
            return instance;
        }

        /// <summary>
        /// 인스턴스화된 GameObject를 해제합니다.
        /// GameObject를 파괴하고 추적에서 제거합니다.
        /// (프리팹 자체는 참조 카운트로 관리되므로 여기서 해제하지 않습니다)
        /// </summary>
        /// <param name="instance">해제할 GameObject 인스턴스</param>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instanceTracker.TryGetAddress(instance, out var address))
            {
                // GameObject 파괴
                UnityEngine.Object.Destroy(instance);

                // 추적에서 제거
                instanceTracker.UntrackInstance(instance);

                Log($"[AddressableManager] 인스턴스 해제: {address}");
            }
            else
            {
                LogWarning($"[AddressableManager] 추적되지 않은 인스턴스입니다. 직접 파괴합니다.");
                UnityEngine.Object.Destroy(instance);
            }
        }

        /// <summary>
        /// 모든 추적 중인 인스턴스를 해제합니다.
        /// </summary>
        public void ReleaseAllInstances()
        {
            var instances = instanceTracker.GetAllInstances();

            foreach (var instance in instances)
            {
                ReleaseInstance(instance);
            }

            Log($"[AddressableManager] 모든 인스턴스 해제 완료 (개수: {instances.Count})");
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
            var taskList = new List<UniTask>();
            int count = 0;
            foreach (var address in addresses)
            {
                taskList.Add(LoadAssetAsync<UnityEngine.Object>(address, ct));
                count++;
            }
            await UniTask.WhenAll(taskList);

            Log($"[AddressableManager] 프리로드 완료 (개수: {count})");
        }

        /// <summary>
        /// 특정 라벨의 모든 리소스를 프리로드합니다.
        /// </summary>
        /// <param name="label">Addressable Label</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadByLabelAsync(string label, CancellationToken ct = default)
        {
            await LoadAssetsAsync<UnityEngine.Object>(label, ct);

            Log($"[AddressableManager] 라벨 프리로드 완료: {label}");
        }

        #endregion

        #region 디버깅

        /// <summary>
        /// 현재 로드된 모든 리소스 정보를 반환합니다.
        /// </summary>
        /// <returns>로드된 리소스 정보 리스트</returns>
        public IReadOnlyList<LoadedAssetInfo> GetLoadedAssets()
        {
            return debugger.GetLoadedAssets();
        }

        /// <summary>
        /// 현재 로드된 리소스 개수를 반환합니다.
        /// </summary>
        public int GetLoadedCount()
        {
            return debugger.GetLoadedCount();
        }

        /// <summary>
        /// 현재 추적 중인 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetInstanceCount()
        {
            return debugger.GetInstanceCount();
        }

        /// <summary>
        /// 디버그 정보를 콘솔에 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            debugger.PrintDebugInfo();
        }

        #endregion

    }
}
