using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// MainCanvas 기반 레이어별 GameObject 관리
    /// MainCanvas(Canvas) 하나에 레이어별 GameObject(RectTransform)가 하위 구조로 존재합니다.
    /// </summary>
    public class UICanvas
    {
        // Enum.GetValues() 캐싱 (성능 최적화)
        private static readonly UILayer[] AllLayers = (UILayer[])System.Enum.GetValues(typeof(UILayer));

        private readonly Dictionary<UILayer, RectTransform> layerTransforms = new Dictionary<UILayer, RectTransform>();
        private Canvas mainCanvas;

        /// <summary>
        /// MainCanvas 프리팹에서 레이어별 GameObject를 찾아서 초기화합니다.
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

            // 레이어별 GameObject 찾기 (Canvas가 아닌 RectTransform만 있으면 됨)
            foreach (UILayer layer in AllLayers)
            {
                string layerName = layer.ToString();
                Transform layerTransform = mainCanvasObject.transform.Find(layerName);

                if (layerTransform != null)
                {
                    RectTransform rectTransform = layerTransform.GetComponent<RectTransform>();

                    if (rectTransform != null)
                    {
                        layerTransforms[layer] = rectTransform;
                    }
                    else
                    {
                        Debug.LogError($"RectTransform component not found on layer {layerName}!");
                    }
                }
                else
                {
                    Debug.LogError($"Layer {layerName} not found in MainCanvas!");
                }
            }
        }

        /// <summary>
        /// 특정 레이어의 MainCanvas를 반환합니다.
        /// 모든 레이어는 하나의 MainCanvas를 공유합니다.
        /// </summary>
        /// <param name="layer">레이어 (사용하지 않지만 하위 호환성 유지)</param>
        /// <returns>MainCanvas</returns>
        public Canvas GetCanvas(UILayer layer)
        {
            // 모든 레이어는 MainCanvas를 공유
            return mainCanvas;
        }

        /// <summary>
        /// 특정 레이어의 Transform을 반환합니다. (UI 부모로 사용)
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>RectTransform</returns>
        public Transform GetCanvasTransform(UILayer layer)
        {
            if (layerTransforms.TryGetValue(layer, out RectTransform rectTransform))
            {
                return rectTransform;
            }

            Debug.LogError($"Layer {layer} not found!");
            return null;
        }
    }
}
