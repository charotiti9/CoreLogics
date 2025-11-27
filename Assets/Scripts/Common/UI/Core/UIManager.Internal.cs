using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Core.Pool;

namespace Common.UI
{
    /// <summary>
    /// UIManager의 내부 헬퍼 메서드 파트
    /// </summary>
    public partial class UIManager
    {
        /// <summary>
        /// UI를 숨깁니다. (내부 비동기 처리)
        /// </summary>
        private async UniTaskVoid HideUIAsync(UIBase ui, bool immediate, CancellationToken ct)
        {
            try
            {
                UIInputBlocker.Instance.Block();

                UILayer layer = ui.Layer;
                Type type = ui.GetType();

                // 스택에서 제거
                uiStack.Remove(ui);

                // UI 숨김
                await ui.HideInternalAsync(immediate, ct);

                // 활성 UI에서 제거
                activeUIs.Remove(type);

                // 풀로 반환 (PoolManager 사용)
                PoolManager.ReturnToPool(ui);

                // Dim 숨김 (UI Stack 지원)
                if (immediate)
                {
                    dimController.ClearDim(layer);
                }
                else
                {
                    await dimController.HideDimAsync(ui, layer, ct);
                }
            }
            finally
            {
                UIInputBlocker.Instance.Unblock();
            }
        }

        /// <summary>
        /// 특정 레이어에 활성화된 UI가 있는지 확인합니다.
        /// </summary>
        private bool HasActiveUIInLayer(UILayer layer)
        {
            foreach (var ui in activeUIs.Values)
            {
                if (ui != null && ui.Layer == layer)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// activeUIs Dictionary에서 null 참조를 정리합니다.
        /// UI가 UIManager를 거치지 않고 직접 파괴된 경우 호출됩니다.
        /// </summary>
        private void CleanupNullReferences()
        {
            var keysToRemove = new List<Type>();
            foreach (var pair in activeUIs)
            {
                if (pair.Value == null)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            if (keysToRemove.Count > 0)
            {
                Debug.LogWarning($"[UIManager] {keysToRemove.Count}개의 null 참조를 정리합니다.");

                foreach (var key in keysToRemove)
                {
                    activeUIs.Remove(key);
                }
            }
        }

        /// <summary>
        /// MainCanvas를 찾거나 Addressable에서 비동기 로드합니다.
        /// </summary>
        private async UniTask<GameObject> FindOrCreateMainCanvasAsync(CancellationToken ct)
        {
            // 1. 씬에서 찾기
            GameObject existing = GameObject.Find("MainCanvas");
            if (existing != null)
            {
                return existing;
            }

            // 2. Addressable에서 로드
            try
            {
                mainCanvasHandle = Addressables.InstantiateAsync(MAIN_CANVAS_ADDRESS);
                GameObject instance = await mainCanvasHandle.ToUniTask(cancellationToken: ct);
                instance.name = "MainCanvas";
                return instance;
            }
            catch (Exception ex)
            {
                // Fallback 생성하지 않고 명확한 에러 처리
                throw new InvalidOperationException(
                    $"[UIManager] MainCanvas를 찾을 수 없습니다.\n\n" +
                    $"원인: {ex.Message}\n\n" +
                    $"해결 방법:\n" +
                    $"1. Addressable Groups에 '{MAIN_CANVAS_ADDRESS}' 프리팹을 등록하세요\n" +
                    $"2. 또는 씬에 'MainCanvas' 이름의 GameObject를 배치하세요\n" +
                    $"3. MainCanvas 구조:\n" +
                    $"   MainCanvas (Canvas, CanvasScaler, GraphicRaycaster)\n" +
                    $"   ├─ Background (GameObject with RectTransform)\n" +
                    $"   ├─ HUD (GameObject with RectTransform)\n" +
                    $"   ├─ Overlay (GameObject with RectTransform)\n" +
                    $"   ├─ PopUp (GameObject with RectTransform)\n" +
                    $"   ├─ System (GameObject with RectTransform)\n" +
                    $"   └─ Transition (GameObject with RectTransform)\n" +
                    $"   주의: 레이어들은 Canvas가 아닌 GameObject여야 합니다!\n\n" +
                    $"Addressable 설정: Window > Asset Management > Addressables > Groups",
                    ex);
            }
        }
    }
}
