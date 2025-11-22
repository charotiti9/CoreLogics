using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// 레이어별 Canvas 관리
    /// 모든 Canvas는 DontDestroyOnLoad로 설정됩니다.
    /// </summary>
    public class UICanvas
    {
        private readonly Dictionary<UILayer, Canvas> canvases = new Dictionary<UILayer, Canvas>();
        private readonly Transform rootTransform;

        /// <summary>
        /// UICanvas를 생성합니다.
        /// </summary>
        /// <param name="rootTransform">Canvas들의 부모 Transform</param>
        public UICanvas(Transform rootTransform)
        {
            this.rootTransform = rootTransform;
        }

        /// <summary>
        /// 모든 레이어의 Canvas를 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            // 모든 레이어에 대해 Canvas 생성
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                CreateCanvas(layer);
            }
        }

        /// <summary>
        /// 특정 레이어의 Canvas를 생성합니다.
        /// </summary>
        private void CreateCanvas(UILayer layer)
        {
            // Canvas GameObject 생성
            GameObject canvasObj = new GameObject($"{layer}Canvas");
            canvasObj.transform.SetParent(rootTransform);

            // Canvas 컴포넌트 추가
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = (int)layer;

            // CanvasScaler 추가 (해상도 대응)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // GraphicRaycaster 추가 (UI 입력 처리)
            canvasObj.AddComponent<GraphicRaycaster>();

            // Dictionary에 등록
            canvases[layer] = canvas;
        }

        /// <summary>
        /// 특정 레이어의 Canvas를 반환합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>Canvas</returns>
        public Canvas GetCanvas(UILayer layer)
        {
            if (canvases.TryGetValue(layer, out Canvas canvas))
            {
                return canvas;
            }

            Debug.LogError($"Canvas for layer {layer} not found!");
            return null;
        }

        /// <summary>
        /// 특정 레이어의 Transform을 반환합니다. (UI 부모로 사용)
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>Transform</returns>
        public Transform GetCanvasTransform(UILayer layer)
        {
            Canvas canvas = GetCanvas(layer);
            return canvas != null ? canvas.transform : null;
        }

        /// <summary>
        /// 특정 레이어의 sortingOrder를 설정합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="order">sortingOrder 값</param>
        public void SetSortingOrder(UILayer layer, int order)
        {
            Canvas canvas = GetCanvas(layer);
            if (canvas != null)
            {
                canvas.sortingOrder = order;
            }
        }
    }
}
