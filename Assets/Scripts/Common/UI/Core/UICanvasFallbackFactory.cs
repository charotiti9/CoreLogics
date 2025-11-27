using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Common.UI
{
    /// <summary>
    /// Fallback MainCanvas 생성을 담당하는 Factory 클래스
    /// Addressable 로드 실패 시 또는 테스트 환경에서 사용됩니다.
    /// </summary>
    public static class UICanvasFallbackFactory
    {
        // Enum.GetValues() 캐싱
        private static readonly UILayer[] AllLayers = (UILayer[])Enum.GetValues(typeof(UILayer));

        /// <summary>
        /// Fallback MainCanvas를 생성합니다.
        /// </summary>
        /// <param name="uiInputActions">UI Input Actions Asset (optional)</param>
        /// <returns>생성된 MainCanvas GameObject</returns>
        public static GameObject CreateMainCanvas(InputActionAsset uiInputActions = null)
        {
            GameObject mainCanvasObj = CreateMainCanvasObject();
            CreateEventSystemIfNeeded(uiInputActions);
            CreateLayerCanvases(mainCanvasObj);

            return mainCanvasObj;
        }

        /// <summary>
        /// MainCanvas GameObject를 생성하고 기본 컴포넌트를 설정합니다.
        /// </summary>
        private static GameObject CreateMainCanvasObject()
        {
            GameObject obj = new GameObject("MainCanvas");

            // Canvas 설정
            Canvas mainCanvas = obj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // CanvasScaler 설정
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster 추가
            obj.AddComponent<GraphicRaycaster>();

            return obj;
        }

        /// <summary>
        /// EventSystem이 없으면 생성합니다.
        /// </summary>
        private static void CreateEventSystemIfNeeded(InputActionAsset uiInputActions)
        {
            // 이미 EventSystem이 있으면 생성하지 않음
            if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystemObj = new GameObject("EventSystem");

            // EventSystem 추가
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

            // InputSystemUIInputModule 추가 및 설정
            var inputModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();

            if (uiInputActions != null)
            {
                inputModule.actionsAsset = uiInputActions;
            }
            else
            {
                Debug.LogWarning("[UICanvasFallbackFactory] UI Input Actions Asset이 할당되지 않았습니다. " +
                    "UIManager Inspector에서 InputSystem_Actions를 할당하세요.");
            }
        }

        /// <summary>
        /// 레이어별 Nested Canvas를 생성합니다.
        /// </summary>
        private static void CreateLayerCanvases(GameObject mainCanvasObj)
        {
            foreach (UILayer layer in AllLayers)
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
        }
    }
}
