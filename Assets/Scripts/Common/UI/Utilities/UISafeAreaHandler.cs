using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// 노치 디스플레이 Safe Area 대응
    /// Safe Area를 적용할 RectTransform에 컴포넌트를 부착하면 자동으로 처리됩니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UISafeAreaHandler : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        /// <summary>
        /// Safe Area를 적용합니다.
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;

            // Safe Area가 변경되지 않았으면 처리하지 않음
            if (safeArea == lastSafeArea)
            {
                return;
            }

            lastSafeArea = safeArea;

            // 화면 크기
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            // Safe Area의 앵커 포인트 계산
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            // 정규화 (0~1 범위)
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;

            // RectTransform에 적용
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

            // 오프셋 초기화 (Safe Area에 딱 맞게)
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// RectTransform의 크기가 변경될 때 호출됩니다.
        /// (해상도 변경, 화면 회전 등)
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }
    }
}
