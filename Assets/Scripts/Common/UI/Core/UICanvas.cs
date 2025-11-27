using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// MainCanvas 기반 레이어별 Nested Canvas 관리
    /// MainCanvas 하나에 레이어별 Canvas가 하위 구조로 존재합니다.
    /// </summary>
    public class UICanvas
    {
        // Enum.GetValues() 캐싱 (성능 최적화)
        private static readonly UILayer[] AllLayers = (UILayer[])System.Enum.GetValues(typeof(UILayer));

        private readonly Dictionary<UILayer, Canvas> layerCanvases = new Dictionary<UILayer, Canvas>();
        private Canvas mainCanvas;
        private Transform rootTransform;

        /// <summary>
        /// UICanvas를 생성합니다.
        /// </summary>
        /// <param name="rootTransform">UIManager의 Transform</param>
        public UICanvas(Transform rootTransform)
        {
            this.rootTransform = rootTransform;
        }

        /// <summary>
        /// MainCanvas 프리팹에서 레이어별 Canvas를 찾아서 초기화합니다.
        /// </summary>
        /// <param name="mainCanvasObject">MainCanvas GameObject</param>
        public void Initialize(GameObject mainCanvasObject = null)
        {
            if (mainCanvasObject == null)
            {
                Debug.LogError("MainCanvas object is null!");
                return;
            }

            // MainCanvas의 Canvas 컴포넌트 가져오기
            mainCanvas = mainCanvasObject.GetComponent<Canvas>();

            if (mainCanvas == null)
            {
                Debug.LogError("Canvas component not found on MainCanvas!");
                return;
            }

            // 레이어별 Canvas 찾기
            foreach (UILayer layer in AllLayers)
            {
                string layerName = layer.ToString();
                Transform layerTransform = mainCanvasObject.transform.Find(layerName);

                if (layerTransform != null)
                {
                    Canvas layerCanvas = layerTransform.GetComponent<Canvas>();

                    if (layerCanvas != null)
                    {
                        layerCanvases[layer] = layerCanvas;
                    }
                    else
                    {
                        Debug.LogError($"Canvas component not found on layer {layerName}!");
                    }
                }
                else
                {
                    Debug.LogError($"Layer {layerName} not found in MainCanvas!");
                }
            }

            Debug.Log($"UICanvas initialized with {layerCanvases.Count} layers");
        }

        /// <summary>
        /// 특정 레이어의 Canvas를 반환합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>Canvas</returns>
        public Canvas GetCanvas(UILayer layer)
        {
            if (layerCanvases.TryGetValue(layer, out Canvas canvas))
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
