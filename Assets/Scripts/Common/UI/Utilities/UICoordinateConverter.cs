using Core.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// Screen 좌표, World 좌표, UI 좌표 간 변환 유틸리티
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
                GameLogger.LogError("Canvas가 null입니다.");
                return Vector2.zero;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                GameLogger.LogError("Canvas의 RectTransform이 null 입니다.");
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
                GameLogger.LogError("RectTransform이 null입니다.");
                return Vector2.zero;
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameLogger.LogError("parent에서 Canvas를 찾지 못했습니다.");
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
        /// 월드 좌표를 UI 좌표로 변환합니다. (CanvasScaler 고려)
        /// 게임 오브젝트 위에 UI를 배치할 때 사용합니다.
        /// </summary>
        /// <param name="worldPos">월드 좌표</param>
        /// <param name="layer">UI 레이어</param>
        /// <param name="worldCamera">월드 카메라 (null이면 Camera.main 사용)</param>
        /// <returns>UI 로컬 좌표 (anchoredPosition)</returns>
        public static Vector3 WorldToUIPosition(Vector3 worldPos, UILayer layer, Camera worldCamera = null)
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                GameLogger.LogError("World camera가 null입니다.");
                return Vector3.zero;
            }

            // 월드 좌표 → 뷰포트 좌표 (0~1)
            Vector3 viewport = worldCamera.WorldToViewportPoint(worldPos);
            viewport.x -= 0.5f;
            viewport.y -= 0.5f;
            viewport.z = 0;

            // Canvas 정보 가져오기
            Canvas canvas = UIManager.Instance.GetCanvas(layer);
            if (canvas == null)
            {
                GameLogger.LogError($"{layer} 레이어의 Canvas를 찾을 수 없습니다.");
                return Vector3.zero;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                GameLogger.LogError($"{layer} 레이어의 CanvasScaler를 찾을 수 없습니다.");
                return Vector3.zero;
            }

            // 뷰포트 좌표 → 스크린 좌표
            Vector3 screenPoint = new Vector3(
                viewport.x * Screen.width,
                viewport.y * Screen.height,
                0
            );

            // CanvasScaler 적용
            switch (scaler.screenMatchMode)
            {
                case CanvasScaler.ScreenMatchMode.MatchWidthOrHeight:
                    float widthRatio = scaler.referenceResolution.x / Screen.width;
                    float heightRatio = scaler.referenceResolution.y / Screen.height;
                    float ratio = scaler.matchWidthOrHeight;
                    float scaleFactor = ratio * widthRatio + (1 - ratio) * heightRatio;
                    screenPoint *= scaleFactor;
                    break;

                case CanvasScaler.ScreenMatchMode.Expand:
                    screenPoint.x *= scaler.referenceResolution.x / Screen.width;
                    screenPoint.y *= scaler.referenceResolution.y / Screen.height;
                    break;

                case CanvasScaler.ScreenMatchMode.Shrink:
                    screenPoint.x /= Screen.width / scaler.referenceResolution.x;
                    screenPoint.y /= Screen.height / scaler.referenceResolution.y;
                    break;
            }

            return screenPoint;
        }

        /// <summary>
        /// 월드 좌표를 UI 좌표로 변환합니다. (간편 버전)
        /// ScreenSpaceOverlay Canvas용입니다.
        /// </summary>
        /// <param name="worldPos">월드 좌표</param>
        /// <param name="canvas">대상 Canvas</param>
        /// <param name="worldCamera">월드 카메라 (null이면 Camera.main 사용)</param>
        /// <returns>UI 로컬 좌표</returns>
        public static Vector2 WorldToUIPositionSimple(Vector3 worldPos, Canvas canvas, Camera worldCamera = null)
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null || canvas == null)
            {
                GameLogger.LogError("Camera 혹은 Canvas가 null입니다.");
                return Vector2.zero;
            }

            // 월드 좌표 → 스크린 좌표
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

            // 스크린 좌표 → UI 로컬 좌표
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 uiPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out uiPos
            );

            return uiPos;
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
