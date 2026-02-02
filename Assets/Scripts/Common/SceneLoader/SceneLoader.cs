using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Core.Utilities;
using Common.UI;

namespace Common.SceneLoader
{
    /// <summary>
    /// 씬 로드를 담당하는 중앙 관리자
    /// 비동기 씬 로드, 로딩 UI, 페이드 효과를 지원합니다.
    /// </summary>
    public class SceneLoader : LazyMonoSingleton<SceneLoader>
    {
        private AsyncOperationHandle<SceneInstance> currentSceneHandle;
        private bool isLoading;

        protected override bool IsPersistent => true;

        /// <summary>
        /// 씬을 비동기로 로드합니다. (기본 옵션)
        /// </summary>
        /// <param name="sceneAddress">Addressable 씬 주소</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask LoadSceneAsync(string sceneAddress, CancellationToken ct)
        {
            await LoadSceneAsync(sceneAddress, SceneTransitionOptions.Default, ct);
        }

        /// <summary>
        /// 씬을 비동기로 로드합니다.
        /// </summary>
        /// <param name="sceneAddress">Addressable 씬 주소</param>
        /// <param name="options">전환 옵션</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask LoadSceneAsync(string sceneAddress, SceneTransitionOptions options, CancellationToken ct)
        {
            if (isLoading)
            {
                GameLogger.LogWarning("[SceneLoader] 이미 씬 로드가 진행 중입니다.");
                return;
            }

            if (string.IsNullOrEmpty(sceneAddress))
            {
                GameLogger.LogError("[SceneLoader] 씬 주소가 비어있습니다.");
                return;
            }

            isLoading = true;

            try
            {
                if (options.WaitForLoadComplete)
                {
                    await LoadWithPreloadAsync(sceneAddress, options, ct);
                }
                else
                {
                    await LoadImmediateAsync(sceneAddress, options, ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                GameLogger.Log("[SceneLoader] 씬 로드가 취소되었습니다.");
                throw;
            }
            catch (System.Exception ex)
            {
                GameLogger.LogError($"[SceneLoader] 씬 로드 실패: {ex.Message}");
                throw;
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// 현재 로딩 중인지 확인합니다.
        /// </summary>
        public bool IsLoading => isLoading;

        /// <summary>
        /// 씬을 백그라운드에서 로드 완료한 후 전환합니다.
        /// 로딩 중 현재 씬이 유지됩니다.
        /// </summary>
        private async UniTask LoadWithPreloadAsync(string sceneAddress, SceneTransitionOptions options, CancellationToken ct)
        {
            SceneFadeUI fadeUI = null;

            // 1. 로딩 UI 표시 (옵션)
            if (options.ShowLoadingUI)
            {
                await ShowLoadingUIAsync(ct);
            }

            // 2. 새 씬 비동기 로드 (활성화 대기)
            var handle = Addressables.LoadSceneAsync(
                sceneAddress,
                UnityEngine.SceneManagement.LoadSceneMode.Single,
                activateOnLoad: false);

            // 로드 완료 대기 (Task 사용)
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLogger.LogError($"[SceneLoader] 씬 로드 실패: {sceneAddress}");
                return;
            }

            // 3. 로드 완료! 이제 전환 시작

            // 4. 페이드 아웃 (옵션)
            if (options.UseFade)
            {
                fadeUI = await ShowFadeUIAsync(ct);
                await fadeUI.FadeOutAsync(options.FadeColor, options.FadeDuration, ct);
            }

            // 5. 이전 씬 핸들 해제 (새 씬 활성화 전)
            if (currentSceneHandle.IsValid())
            {
                var unloadHandle = Addressables.UnloadSceneAsync(currentSceneHandle);
                await unloadHandle.Task;
            }

            // 6. 새 씬 활성화
            var activateOp = handle.Result.ActivateAsync();
            while (!activateOp.isDone)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            currentSceneHandle = handle;

            // 7. 로딩 UI 숨김 (옵션)
            if (options.ShowLoadingUI)
            {
                HideLoadingUI();
            }

            // 8. 페이드 인 (옵션) - 씬 전환 후이므로 외부 토큰 사용 안 함
            if (options.UseFade && fadeUI != null)
            {
                await fadeUI.FadeInAsync(options.FadeDuration, CancellationToken.None);
                HideFadeUI();
            }
        }

        /// <summary>
        /// 씬을 즉시 전환합니다.
        /// </summary>
        private async UniTask LoadImmediateAsync(string sceneAddress, SceneTransitionOptions options, CancellationToken ct)
        {
            SceneFadeUI fadeUI = null;

            // 1. 페이드 아웃 (옵션)
            if (options.UseFade)
            {
                fadeUI = await ShowFadeUIAsync(ct);
                await fadeUI.FadeOutAsync(options.FadeColor, options.FadeDuration, ct);
            }

            // 2. 로딩 UI 표시 (옵션)
            if (options.ShowLoadingUI)
            {
                await ShowLoadingUIAsync(ct);
            }

            // 3. 이전 씬 핸들 해제
            if (currentSceneHandle.IsValid())
            {
                var unloadHandle = Addressables.UnloadSceneAsync(currentSceneHandle);
                await unloadHandle.Task;
            }

            // 4. 새 씬 로드 및 즉시 활성화
            currentSceneHandle = Addressables.LoadSceneAsync(
                sceneAddress,
                UnityEngine.SceneManagement.LoadSceneMode.Single,
                activateOnLoad: true);

            // 로드 완료 대기 (Task 사용 - 씬 전환 중 UniTask PlayerLoop 중단 방지)
            await currentSceneHandle.Task;

            if (currentSceneHandle.Status != AsyncOperationStatus.Succeeded)
            {
                GameLogger.LogError($"[SceneLoader] 씬 로드 실패: {sceneAddress}");
                return;
            }

            // 5. 로딩 UI 숨김 (옵션)
            if (options.ShowLoadingUI)
            {
                HideLoadingUI();
            }
            
            // 6. 페이드 인 (옵션) - 씬 전환 후이므로 외부 토큰 사용 안 함
            if (options.UseFade && fadeUI != null)
            {
                await fadeUI.FadeInAsync(options.FadeDuration, CancellationToken.None);
                HideFadeUI();
            }
        }

        /// <summary>
        /// 페이드 UI를 표시합니다.
        /// </summary>
        private async UniTask<SceneFadeUI> ShowFadeUIAsync(CancellationToken ct)
        {
            if (!UIManager.IsAlive())
            {
                GameLogger.LogWarning("[SceneLoader] UIManager가 초기화되지 않아 페이드 UI를 표시할 수 없습니다.");
                return null;
            }

            if(!UIManager.Instance.IsSpawned<SceneFadeUI>())
            {
                await UIManager.Instance.SpawnAsync<SceneFadeUI>();
            }
            return await UIManager.Instance.ShowAsync<SceneFadeUI>(ct: ct);
        }

        /// <summary>
        /// 페이드 UI를 숨깁니다.
        /// </summary>
        private void HideFadeUI()
        {
            if (!UIManager.IsAlive())
            {
                return;
            }

            UIManager.Instance.Hide<SceneFadeUI>(immediate: true);
        }

        /// <summary>
        /// 로딩 UI를 표시합니다.
        /// </summary>
        private async UniTask ShowLoadingUIAsync(CancellationToken ct)
        {
            if (!UIManager.IsAlive())
            {
                GameLogger.LogWarning("[SceneLoader] UIManager가 초기화되지 않아 로딩 UI를 표시할 수 없습니다.");
                return;
            }
            
            if(!UIManager.Instance.IsSpawned<LoadingUI>())
            {
                await UIManager.Instance.SpawnAsync<LoadingUI>();
            }
            await UIManager.Instance.ShowAsync<LoadingUI>(ct: ct);
        }

        /// <summary>
        /// 로딩 UI를 숨깁니다.
        /// </summary>
        private void HideLoadingUI()
        {
            if (!UIManager.IsAlive())
            {
                return;
            }

            UIManager.Instance.Hide<LoadingUI>(immediate: true);
        }
    }
}
