using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// Dim 효과(배경 어둡게 하기) 관리
    /// 각 레이어별로 독립적인 Dim을 관리합니다.
    /// </summary>
    public class UIDimController
    {
        private readonly Dictionary<UILayer, GameObject> dimObjects = new Dictionary<UILayer, GameObject>();
        private readonly UICanvas uiCanvas;

        // Dim 설정
        private const float DIM_FADE_DURATION = 0.2f; // 페이드 지속 시간

        /// <summary>
        /// UIDimController를 생성합니다.
        /// </summary>
        /// <param name="uiCanvas">UICanvas 인스턴스</param>
        public UIDimController(UICanvas uiCanvas)
        {
            this.uiCanvas = uiCanvas;
        }

        /// <summary>
        /// Dim을 표시합니다. (페이드 인)
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="alpha">Dim 투명도 (0~1)</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask ShowDimAsync(UILayer layer, float alpha = 0.7f, CancellationToken ct = default)
        {
            // 이미 Dim이 있으면 알파값만 업데이트
            if (dimObjects.TryGetValue(layer, out GameObject existingDim) && existingDim != null)
            {
                CanvasGroup canvasGroup = existingDim.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    await FadeAsync(canvasGroup, canvasGroup.alpha, alpha, ct);
                }
                return;
            }

            // Dim GameObject 생성
            GameObject dimObj = CreateDim(layer);
            dimObjects[layer] = dimObj;

            // 페이드 인 애니메이션
            CanvasGroup dimCanvasGroup = dimObj.GetComponent<CanvasGroup>();
            await FadeAsync(dimCanvasGroup, 0f, alpha, ct);
        }

        /// <summary>
        /// Dim을 숨깁니다. (페이드 아웃)
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask HideDimAsync(UILayer layer, CancellationToken ct = default)
        {
            if (!dimObjects.TryGetValue(layer, out GameObject dimObj) || dimObj == null)
            {
                return; // Dim이 없음
            }

            // 페이드 아웃 애니메이션
            CanvasGroup canvasGroup = dimObj.GetComponent<CanvasGroup>();
            await FadeAsync(canvasGroup, canvasGroup.alpha, 0f, ct);

            // Dim 제거
            GameObject.Destroy(dimObj);
            dimObjects.Remove(layer);
        }

        /// <summary>
        /// Dim을 즉시 제거합니다. (애니메이션 없음)
        /// </summary>
        /// <param name="layer">레이어</param>
        public void ClearDim(UILayer layer)
        {
            if (dimObjects.TryGetValue(layer, out GameObject dimObj) && dimObj != null)
            {
                GameObject.Destroy(dimObj);
                dimObjects.Remove(layer);
            }
        }

        /// <summary>
        /// 해당 레이어에 Dim이 있는지 확인합니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>Dim 존재 여부</returns>
        public bool HasDim(UILayer layer)
        {
            return dimObjects.ContainsKey(layer) && dimObjects[layer] != null;
        }

        /// <summary>
        /// Dim GameObject를 생성합니다.
        /// </summary>
        private GameObject CreateDim(UILayer layer)
        {
            Transform parent = uiCanvas.GetCanvasTransform(layer);

            GameObject dimObj = new GameObject($"Dim_{layer}");
            dimObj.transform.SetParent(parent, false);

            // RectTransform 설정 (전체 화면)
            RectTransform rectTransform = dimObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            // Image 컴포넌트 추가 (검은색)
            Image image = dimObj.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 1); // 검은색 (알파는 CanvasGroup으로 조절)

            // CanvasGroup 추가 (페이드 애니메이션용)
            CanvasGroup canvasGroup = dimObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // 시작은 투명

            // Button 추가 (배경 클릭 감지, 선택적)
            // dimObj.AddComponent<Button>();

            // 가장 아래에 배치 (UI 뒤에 표시)
            rectTransform.SetAsFirstSibling();

            return dimObj;
        }

        /// <summary>
        /// 페이드 애니메이션
        /// </summary>
        private async UniTask FadeAsync(CanvasGroup canvasGroup, float from, float to, CancellationToken ct)
        {
            if (canvasGroup == null)
            {
                return;
            }

            float elapsed = 0f;
            while (elapsed < DIM_FADE_DURATION)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DIM_FADE_DURATION);
                canvasGroup.alpha = Mathf.Lerp(from, to, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            canvasGroup.alpha = to;
        }

        /// <summary>
        /// 모든 Dim을 제거합니다.
        /// </summary>
        public void ClearAll()
        {
            foreach (var dimObj in dimObjects.Values)
            {
                if (dimObj != null)
                {
                    GameObject.Destroy(dimObj);
                }
            }
            dimObjects.Clear();
        }
    }
}
