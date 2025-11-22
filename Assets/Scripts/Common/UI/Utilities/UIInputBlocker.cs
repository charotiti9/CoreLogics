using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// UI 전환 중 입력 차단
    /// 전체 화면을 덮는 투명한 패널로 모든 입력을 차단합니다.
    /// </summary>
    public class UIInputBlocker : MonoBehaviour
    {
        private static UIInputBlocker instance;
        private static GameObject blockerObject;
        private static int blockCount = 0; // 중첩 차단 카운트

        /// <summary>
        /// 현재 입력이 차단된 상태인지 여부
        /// </summary>
        public static bool IsBlocked => blockCount > 0;

        /// <summary>
        /// 입력을 차단합니다.
        /// 중첩 호출을 지원합니다. (Block 2번 호출 시 Unblock도 2번 호출해야 함)
        /// </summary>
        public static void Block()
        {
            blockCount++;

            if (blockCount == 1)
            {
                CreateBlocker();
            }
        }

        /// <summary>
        /// 입력 차단을 해제합니다.
        /// </summary>
        public static void Unblock()
        {
            if (blockCount > 0)
            {
                blockCount--;
            }

            if (blockCount == 0)
            {
                DestroyBlocker();
            }
        }

        /// <summary>
        /// 강제로 입력 차단을 해제합니다. (모든 중첩 차단 무시)
        /// </summary>
        public static void ForceUnblock()
        {
            blockCount = 0;
            DestroyBlocker();
        }

        /// <summary>
        /// Blocker 생성
        /// </summary>
        private static void CreateBlocker()
        {
            if (blockerObject != null)
            {
                return; // 이미 존재함
            }

            // 최상위 Canvas 찾기 (Transition 레이어가 가장 높음)
            Canvas targetCanvas = FindHighestCanvas();

            if (targetCanvas == null)
            {
                Debug.LogWarning("No canvas found for UIInputBlocker!");
                return;
            }

            // Blocker GameObject 생성
            blockerObject = new GameObject("InputBlocker");
            blockerObject.transform.SetParent(targetCanvas.transform, false);

            // RectTransform 설정 (전체 화면)
            RectTransform rectTransform = blockerObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // 투명한 Image 추가 (Raycast 대상)
            Image image = blockerObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0); // 완전 투명

            // 최상위로 이동
            rectTransform.SetAsLastSibling();
        }

        /// <summary>
        /// Blocker 제거
        /// </summary>
        private static void DestroyBlocker()
        {
            if (blockerObject != null)
            {
                Destroy(blockerObject);
                blockerObject = null;
            }
        }

        /// <summary>
        /// 가장 높은 sortingOrder를 가진 Canvas 찾기
        /// </summary>
        private static Canvas FindHighestCanvas()
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            Canvas highestCanvas = null;
            int highestOrder = int.MinValue;

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.sortingOrder > highestOrder)
                {
                    highestOrder = canvas.sortingOrder;
                    highestCanvas = canvas;
                }
            }

            return highestCanvas;
        }

        /// <summary>
        /// 씬 전환 시 정리
        /// </summary>
        private void OnDestroy()
        {
            if (instance == this)
            {
                ForceUnblock();
                instance = null;
            }
        }
    }
}
