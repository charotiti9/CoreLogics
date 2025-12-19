using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Core.Utilities;

namespace Common.UI
{
    /// <summary>
    /// UIManager의 내부 헬퍼 메서드 파트
    /// </summary>
    public partial class UIManager
    {
        /// <summary>
        /// UIAddress Attribute에서 주소 가져오기
        /// </summary>
        private string GetUIAddress(Type uiType)
        {
            var attribute = uiType.GetCustomAttribute<UIAddressAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"[UIManager] {uiType.Name}에 UIAddress Attribute가 없습니다.\n" +
                    $"예시: [UIAddress(\"Assets/Scripts/Common/UI/MyUI.prefab\")]");
            }
            return attribute.Address;
        }

        /// <summary>
        /// UI 인스턴스를 생성합니다 (내부 구현).
        /// </summary>
        private async UniTask<T> SpawnUIAsync<T>(CancellationToken ct) where T : UIBase
        {
            Type type = typeof(T);

            // UIAddress Attribute에서 주소 가져오기
            string address = GetUIAddress(type);

            // Addressable 로드 및 Instantiate
            var handle = Addressables.InstantiateAsync(address);
            GameObject uiObj = await handle.ToUniTask(cancellationToken: ct);

            if (uiObj == null)
            {
                GameLogger.LogError($"{type.Name} UI를 로드하는 데에 실패했습니다.");
                return null;
            }

            T ui = uiObj.GetComponent<T>();
            if (ui == null)
            {
                GameLogger.LogError($"{type.Name} 컴포넌트를 찾을 수 없습니다.");
                Addressables.ReleaseInstance(handle);
                return null;
            }

            // Canvas Layer로 이동
            UILayer targetLayer = ui.Layer;
            Transform canvasLayer = uiCanvas.GetCanvasTransform(targetLayer);
            ui.transform.SetParent(canvasLayer, false);

            // 초기 상태: 비활성화
            uiObj.SetActive(false);

            // Spawn 처리
            ui.SpawnInternal();
            ui.OnSpawn();

            // Dictionary 등록
            spawnedUIs[type] = ui;
            uiHandles[type] = handle;

            return ui;
        }

        /// <summary>
        /// UI 인스턴스를 파괴합니다 (내부 구현).
        /// </summary>
        private void DestroyUI(UIBase ui)
        {
            Type type = ui.GetType();

            // 정리 작업
            ui.OnBeforeDestroy();
            ui.DestroyInternal();

            // Dictionary에서 제거
            spawnedUIs.Remove(type);

            // Addressable 해제
            if (uiHandles.TryGetValue(type, out var handle) && handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
                uiHandles.Remove(type);
            }
        }
        /// <summary>
        /// UI를 숨깁니다. (내부 비동기 처리)
        /// UIBase.UseDim 프로퍼티에 따라 자동으로 Dim 처리합니다.
        /// </summary>
        private async UniTaskVoid HideUIAsync(UIBase ui, bool immediate, CancellationToken ct)
        {
            try
            {
                UIInputBlocker.Instance.Block();

                UILayer layer = ui.Layer;
                Type type = ui.GetType();

                // 스택에서 제거
                uiStack.Remove(ui);

                // UI 숨김 (SetActive(false) + 애니메이션)
                await ui.HideInternalAsync(immediate, ct);

                // 활성 UI에서 제거 (spawnedUIs에는 남아있음!)
                activeUIs.Remove(type);

                // *** 풀로 반환하지 않음! ***
                // 기존: PoolManager.ReturnToPool(ui);  <- 제거!

                // Dim 숨김 (UIBase.UseDim 프로퍼티 사용)
                if (ui.UseDim)
                {
                    if (immediate)
                    {
                        dimController.ClearDim(layer);
                    }
                    else
                    {
                        await dimController.HideDimAsync(ui, layer, ct);
                    }
                }
            }
            finally
            {
                UIInputBlocker.Instance.Unblock();
            }
        }

        /// <summary>
        /// 특정 레이어에 활성화된 UI가 있는지 확인합니다.
        /// </summary>
        private bool HasActiveUIInLayer(UILayer layer)
        {
            foreach (var ui in activeUIs.Values)
            {
                if (ui != null && ui.Layer == layer)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// spawnedUIs와 activeUIs Dictionary에서 null 참조를 정리합니다.
        /// UI가 UIManager를 거치지 않고 직접 파괴된 경우 호출됩니다.
        /// </summary>
        private void CleanupNullReferences()
        {
            var activeKeysToRemove = new List<Type>();
            foreach (var pair in activeUIs)
            {
                if (pair.Value == null)
                {
                    activeKeysToRemove.Add(pair.Key);
                }
            }

            var spawnedKeysToRemove = new List<Type>();
            foreach (var pair in spawnedUIs)
            {
                if (pair.Value == null)
                {
                    spawnedKeysToRemove.Add(pair.Key);
                }
            }

            int totalCount = activeKeysToRemove.Count + spawnedKeysToRemove.Count;
            if (totalCount > 0)
            {
                GameLogger.LogWarning($"[UIManager] {totalCount}개의 null 참조를 정리합니다.");

                foreach (var key in activeKeysToRemove)
                {
                    activeUIs.Remove(key);
                }

                foreach (var key in spawnedKeysToRemove)
                {
                    spawnedUIs.Remove(key);

                    // Addressable 핸들도 해제
                    if (uiHandles.TryGetValue(key, out var handle) && handle.IsValid())
                    {
                        Addressables.ReleaseInstance(handle);
                        uiHandles.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// MainCanvas를 찾거나 Addressable에서 비동기 로드합니다.
        /// </summary>
        private async UniTask<GameObject> FindOrCreateMainCanvasAsync(CancellationToken ct)
        {
            // 1. 씬에서 찾기
            GameObject existing = GameObject.Find("MainCanvas");
            if (existing != null)
            {
                return existing;
            }

            // 2. Addressable에서 로드
            try
            {
                mainCanvasHandle = Addressables.InstantiateAsync(MAIN_CANVAS_ADDRESS);
                GameObject instance = await mainCanvasHandle.ToUniTask(cancellationToken: ct);
                instance.name = "MainCanvas";
                return instance;
            }
            catch (Exception ex)
            {
                // Fallback 생성하지 않고 명확한 에러 처리
                throw new InvalidOperationException(
                    $"[UIManager] MainCanvas를 찾을 수 없습니다.\n\n" +
                    $"원인: {ex.Message}\n\n" +
                    $"해결 방법:\n" +
                    $"1. Addressable Groups에 '{MAIN_CANVAS_ADDRESS}' 프리팹을 등록하세요\n" +
                    $"2. 또는 씬에 'MainCanvas' 이름의 GameObject를 배치하세요\n" +
                    $"3. MainCanvas 구조:\n" +
                    $"   MainCanvas (Canvas, CanvasScaler, GraphicRaycaster)\n" +
                    $"   ├─ Background (GameObject with RectTransform)\n" +
                    $"   ├─ HUD (GameObject with RectTransform)\n" +
                    $"   ├─ Overlay (GameObject with RectTransform)\n" +
                    $"   ├─ PopUp (GameObject with RectTransform)\n" +
                    $"   ├─ System (GameObject with RectTransform)\n" +
                    $"   └─ Transition (GameObject with RectTransform)\n" +
                    $"   주의: 레이어들은 Canvas가 아닌 GameObject여야 합니다!\n\n" +
                    $"Addressable 설정: Window > Asset Management > Addressables > Groups",
                    ex);
            }
        }
    }
}
