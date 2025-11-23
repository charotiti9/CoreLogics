using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Common.UI
{
    /// <summary>
    /// UIManager의 내부 헬퍼 메서드 파트
    /// </summary>
    public partial class UIManager
    {
        /// <summary>
        /// UI를 숨깁니다. (내부 비동기 처리)
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

                // UI 숨김
                await ui.HideInternalAsync(immediate, ct);

                // 활성 UI에서 제거
                activeUIs.Remove(type);

                // 풀로 반환
                uiPool.Return(ui);

                // Dim 숨김 (UI Stack 지원)
                if (immediate)
                {
                    dimController.ClearDim(layer);
                }
                else
                {
                    await dimController.HideDimAsync(ui, layer, ct);
                }
            }
            finally
            {
                UIInputBlocker.Instance.Unblock();
            }
        }

        /// <summary>
        /// UIPath Attribute에서 Addressable Address를 가져옵니다.
        /// </summary>
        private string GetUIPath<T>() where T : UIBase
        {
            Type type = typeof(T);
            UIPathAttribute attribute = (UIPathAttribute)Attribute.GetCustomAttribute(type, typeof(UIPathAttribute));
            return attribute?.AddressableName;
        }

        /// <summary>
        /// 특정 레이어에 활성화된 UI가 있는지 확인합니다.
        /// </summary>
        private bool HasActiveUIInLayer(UILayer layer)
        {
            return activeUIs.Values.Any(ui => ui != null && ui.Layer == layer);
        }

        /// <summary>
        /// activeUIs Dictionary에서 null 참조를 정리합니다.
        /// UI가 UIManager를 거치지 않고 직접 파괴된 경우 호출됩니다.
        /// </summary>
        private void CleanupNullReferences()
        {
            var keysToRemove = activeUIs.Where(pair => pair.Value == null)
                                         .Select(pair => pair.Key)
                                         .ToList();

            if (keysToRemove.Count > 0)
            {
                Debug.LogWarning($"[UIManager] {keysToRemove.Count}개의 null 참조를 정리합니다.");

                foreach (var key in keysToRemove)
                {
                    activeUIs.Remove(key);
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
                Debug.LogError($"Failed to load MainCanvas from Addressables: {ex.Message}");
            }

            // 3. 실패 시 Fallback 생성
            Debug.LogWarning("Creating fallback MainCanvas...");
            return CreateFallbackMainCanvas();
        }

        /// <summary>
        /// 씬에서 MainCanvas를 찾거나 Fallback을 생성합니다. (동기)
        /// </summary>
        private GameObject FindExistingOrCreateFallback()
        {
            // 씬에서 찾기
            GameObject existing = GameObject.Find("MainCanvas");
            if (existing != null)
            {
                return existing;
            }

            // Fallback 생성
            Debug.LogWarning("Creating fallback MainCanvas...");
            return CreateFallbackMainCanvas();
        }

        /// <summary>
        /// Fallback MainCanvas 생성 (Addressable 로드 실패 시)
        /// </summary>
        private GameObject CreateFallbackMainCanvas()
        {
            GameObject mainCanvasObj = new GameObject("MainCanvas");

            // Main Canvas 설정
            Canvas mainCanvas = mainCanvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = mainCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            mainCanvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem 생성 (이미 존재하지 않는 경우)
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");

                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

                // InputSystemUIInputModule 추가 및 설정
                var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();

                // Input Actions Asset 연결
                if (uiInputActions != null)
                {
                    inputModule.actionsAsset = uiInputActions;
                }
                else
                {
                    Debug.LogWarning("[UIManager] UI Input Actions Asset이 할당되지 않았습니다. UIManager Inspector에서 InputSystem_Actions를 할당하세요.");
                }
            }

            // 레이어별 Nested Canvas 생성
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerObj = new GameObject(layer.ToString());
                layerObj.transform.SetParent(mainCanvasObj.transform);

                // RectTransform 설정 (Full Screen)
                RectTransform rect = layerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Nested Canvas 설정
                Canvas layerCanvas = layerObj.AddComponent<Canvas>();
                layerCanvas.overrideSorting = true;
                layerCanvas.sortingOrder = (int)layer;

                // GraphicRaycaster 추가
                layerObj.AddComponent<GraphicRaycaster>();
            }

            return mainCanvasObj;
        }
    }
}
