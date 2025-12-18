using Core.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// UI 전환 중 입력 차단
    /// 전체 화면을 덮는 투명한 패널로 모든 입력을 차단합니다.
    /// LazyMonoSingleton을 사용하여 자동 생성 및 관리됩니다.
    /// </summary>
    public class UIInputBlocker : LazyMonoSingleton<UIInputBlocker>
    {
        private GameObject blockerObject;
        private int blockCount = 0; // 중첩 차단 카운트
        private Canvas targetCanvas; // UIManager에서 주입받는 Canvas

        /// <summary>
        /// 현재 입력이 차단된 상태인지 여부
        /// </summary>
        public static bool IsBlocked => Instance != null && Instance.blockCount > 0;

        /// <summary>
        /// UIManager에서 최상위 Canvas를 설정합니다.
        /// </summary>
        /// <param name="canvas">최상위 Canvas (일반적으로 Transition 레이어)</param>
        public void SetTargetCanvas(Canvas canvas)
        {
            targetCanvas = canvas;
        }

        /// <summary>
        /// 입력을 차단합니다.
        /// 중첩 호출을 지원합니다. (Block 2번 호출 시 Unblock도 2번 호출해야 함)
        /// </summary>
        public void Block()
        {
            if (targetCanvas == null)
            {
                GameLogger.LogError("[UIInputBlocker] Canvas가 설정되지 않았습니다. UIManager.InitializeAsync()를 먼저 호출하세요.");
                return;
            }

            blockCount++;

            if (blockCount == 1)
            {
                CreateBlocker();
            }
        }

        /// <summary>
        /// 입력 차단을 해제합니다.
        /// </summary>
        public void Unblock()
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
        public void ForceUnblock()
        {
            blockCount = 0;
            DestroyBlocker();
        }

        /// <summary>
        /// Blocker 생성
        /// </summary>
        private void CreateBlocker()
        {
            if (blockerObject != null)
            {
                return; // 이미 존재함
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
        private void DestroyBlocker()
        {
            if (blockerObject != null)
            {
                Destroy(blockerObject);
                blockerObject = null;
            }
        }

        /// <summary>
        /// 씬 전환 시 정리
        /// </summary>
        protected override void OnDestroy()
        {
            ForceUnblock();
            base.OnDestroy();
        }
    }
}
