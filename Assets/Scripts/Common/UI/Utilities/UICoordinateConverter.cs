using UnityEngine;
using UnityEngine.EventSystems;

namespace Common.UI
{
    /// <summary>
    /// Screen 좌표와 UI 좌표 간 변환 유틸리티
    /// </summary>
    public static class UICoordinateConverter
    {
        /// <summary>
        /// 스크린 좌표를 UI 좌표로 변환합니다.
        /// </summary>
        /// <param name="screenPos">스크린 좌표</param>
        /// <param name="canvas">대상 Canvas</param>
        /// <param name="camera">카메라 (null이면 현재 카메라 사용)</param>
        /// <returns>UI 좌표</returns>
        public static Vector2 ScreenToUIPosition(Vector2 screenPos, Canvas canvas, Camera camera = null)
        {
            if (canvas == null)
            {
                Debug.LogError("Canvas is null!");
                return Vector2.zero;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("Canvas RectTransform is null!");
                return Vector2.zero;
            }

            // Canvas의 RenderMode에 따라 카메라 결정
            Camera usedCamera = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                usedCamera = null; // Overlay 모드는 카메라 불필요
            }
            else
            {
                usedCamera = camera != null ? camera : canvas.worldCamera;
            }

            // 스크린 좌표를 Canvas 로컬 좌표로 변환
            Vector2 uiPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                usedCamera,
                out uiPos
            );

            return uiPos;
        }

        /// <summary>
        /// UI 좌표를 스크린 좌표로 변환합니다.
        /// </summary>
        /// <param name="uiPos">UI 좌표</param>
        /// <param name="rectTransform">대상 RectTransform</param>
        /// <param name="camera">카메라 (null이면 현재 카메라 사용)</param>
        /// <returns>스크린 좌표</returns>
        public static Vector2 UIToScreenPosition(Vector2 uiPos, RectTransform rectTransform, Camera camera = null)
        {
            if (rectTransform == null)
            {
                Debug.LogError("RectTransform is null!");
                return Vector2.zero;
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found in parent!");
                return Vector2.zero;
            }

            // Canvas의 RenderMode에 따라 카메라 결정
            Camera usedCamera = null;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                usedCamera = null;
            }
            else
            {
                usedCamera = camera != null ? camera : canvas.worldCamera;
            }

            // 월드 좌표로 변환 후 스크린 좌표로 변환
            Vector3 worldPos = rectTransform.TransformPoint(uiPos);

            if (usedCamera == null)
            {
                // Overlay 모드
                return worldPos;
            }
            else
            {
                // Camera 모드
                return usedCamera.WorldToScreenPoint(worldPos);
            }
        }

        /// <summary>
        /// 현재 포인터가 UI 위에 있는지 확인합니다.
        /// </summary>
        /// <returns>UI 위에 있으면 true</returns>
        public static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
