using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// Dim 효과(배경 어둡게 하기) 관리
    /// 각 레이어별로 독립적인 Dim을 관리하며, UI Stack을 지원합니다.
    /// </summary>
    public class UIDimController
    {
        private readonly Dictionary<UILayer, GameObject> dimObjects = new Dictionary<UILayer, GameObject>();
        private readonly Dictionary<UILayer, List<UIBase>> dimUIStacks = new Dictionary<UILayer, List<UIBase>>();
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
        /// UI Stack을 지원하여 중첩된 팝업에서도 올바르게 동작합니다.
        /// </summary>
        /// <param name="ui">Dim을 사용하는 UI (Stack 관리용)</param>
        /// <param name="layer">레이어</param>
        /// <param name="alpha">Dim 투명도 (0~1)</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask ShowDimAsync(UIBase ui, UILayer layer, float alpha = 0.7f, CancellationToken ct = default)
        {
            // UI Stack에 추가
            if (!dimUIStacks.ContainsKey(layer))
            {
                dimUIStacks[layer] = new List<UIBase>();
            }

            // null 참조 정리 (UI가 직접 파괴된 경우)
            dimUIStacks[layer].RemoveAll(item => item == null);

            if (ui != null && !dimUIStacks[layer].Contains(ui))
            {
                dimUIStacks[layer].Add(ui);
            }

            // Dim GameObject 생성 또는 가져오기
            if (!dimObjects.TryGetValue(layer, out GameObject dimObj) || dimObj == null)
            {
                dimObj = CreateDim(layer);
                dimObjects[layer] = dimObj;
            }

            // Dim을 UI 바로 아래로 이동 (Stack 최상단 UI 기준)
            if (ui != null)
            {
                Transform uiTransform = ui.transform;
                Transform dimTransform = dimObj.transform;
                dimTransform.SetParent(uiTransform.parent);

                int siblingIndex = uiTransform.GetSiblingIndex();
                if (siblingIndex <= 0)
                {
                    dimTransform.SetAsFirstSibling();
                }
                else
                {
                    dimTransform.SetSiblingIndex(siblingIndex - 1);
                }
            }

            // 페이드 인 애니메이션
            CanvasGroup dimCanvasGroup = dimObj.GetComponent<CanvasGroup>();
            if (dimCanvasGroup != null)
            {
                await FadeAsync(dimCanvasGroup, dimCanvasGroup.alpha, alpha, ct);
            }
        }

        /// <summary>
        /// Dim을 표시합니다. (UI 없이 레이어만 지정)
        /// 하위 호환성을 위해 유지합니다.
        /// </summary>
        public async UniTask ShowDimAsync(UILayer layer, float alpha = 0.7f, CancellationToken ct = default)
        {
            await ShowDimAsync(null, layer, alpha, ct);
        }

        /// <summary>
        /// Dim을 숨깁니다. (페이드 아웃)
        /// UI Stack을 관리하여 중첩된 팝업에서도 올바르게 동작합니다.
        /// </summary>
        /// <param name="ui">Dim을 사용한 UI (Stack 관리용)</param>
        /// <param name="layer">레이어</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask HideDimAsync(UIBase ui, UILayer layer, CancellationToken ct = default)
        {
            if (!dimUIStacks.ContainsKey(layer))
            {
                return;
            }

            // UI Stack에서 제거
            var stack = dimUIStacks[layer];

            // null 참조 정리 (UI가 직접 파괴된 경우)
            stack.RemoveAll(item => item == null);

            int index = stack.IndexOf(ui);

            if (index != -1)
            {
                stack.RemoveAt(index);

                // 스택에 다른 UI가 남아있으면 그 아래로 Dim 이동
                if (stack.Count > 0)
                {
                    // 최상단 UI 찾기 (null이 아닌 것)
                    UIBase prevUI = null;
                    for (int i = stack.Count - 1; i >= 0; i--)
                    {
                        if (stack[i] != null)
                        {
                            prevUI = stack[i];
                            break;
                        }
                    }

                    if (prevUI != null && dimObjects.TryGetValue(layer, out GameObject dimObj) && dimObj != null)
                    {
                        Transform uiTransform = prevUI.transform;
                        Transform dimTransform = dimObj.transform;
                        dimTransform.SetParent(uiTransform.parent);

                        int siblingIndex = uiTransform.GetSiblingIndex();
                        if (siblingIndex <= 0)
                        {
                            dimTransform.SetAsFirstSibling();
                        }
                        else
                        {
                            dimTransform.SetSiblingIndex(siblingIndex - 1);
                        }
                    }
                    else
                    {
                        // 유효한 UI가 없으면 Dim 숨김
                        await HideDimCompletelyAsync(layer, ct);
                    }
                }
                else
                {
                    // 스택이 비었으면 Dim 숨김
                    await HideDimCompletelyAsync(layer, ct);
                }
            }
        }

        /// <summary>
        /// Dim을 완전히 숨깁니다. (내부용)
        /// </summary>
        private async UniTask HideDimCompletelyAsync(UILayer layer, CancellationToken ct)
        {
            if (!dimObjects.TryGetValue(layer, out GameObject dimObj) || dimObj == null)
            {
                return;
            }

            // 페이드 아웃 애니메이션
            CanvasGroup canvasGroup = dimObj.GetComponent<CanvasGroup>();
            await FadeAsync(canvasGroup, canvasGroup.alpha, 0f, ct);

            // Dim 제거
            GameObject.Destroy(dimObj);
            dimObjects.Remove(layer);
        }

        /// <summary>
        /// Dim을 숨깁니다. (UI 없이 레이어만 지정)
        /// 하위 호환성을 위해 유지합니다.
        /// </summary>
        public async UniTask HideDimAsync(UILayer layer, CancellationToken ct = default)
        {
            await HideDimCompletelyAsync(layer, ct);

            // 스택 정리
            if (dimUIStacks.ContainsKey(layer))
            {
                dimUIStacks[layer].Clear();
            }
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

            // 스택 정리
            if (dimUIStacks.ContainsKey(layer))
            {
                dimUIStacks[layer].Clear();
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

            // 모든 스택 정리
            foreach (var stack in dimUIStacks.Values)
            {
                stack.Clear();
            }
            dimUIStacks.Clear();
        }
    }
}
