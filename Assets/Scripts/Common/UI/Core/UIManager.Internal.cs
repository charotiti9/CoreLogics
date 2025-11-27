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
                Debug.LogError($"Failed to load MainCanvas from Addressables: {ex.Message}");
            }

            // 3. 실패 시 Fallback 생성
            Debug.LogWarning("Creating fallback MainCanvas...");
            return CreateFallbackMainCanvas();
        }

        /// <summary>
        /// 씬에서 MainCanvas를 찾거나 Fallback을 생성합니다. (동기)
        /// </summary>
        private GameObject FindExistingOrCreateFallback()
        {
            // 씬에서 찾기
            GameObject existing = GameObject.Find("MainCanvas");
            if (existing != null)
            {
                return existing;
            }

            // Fallback 생성
            Debug.LogWarning("Creating fallback MainCanvas...");
            return CreateFallbackMainCanvas();
        }

        /// <summary>
        /// Fallback MainCanvas 생성 (Addressable 로드 실패 시)
        /// UICanvasFallbackFactory에 위임합니다.
        /// </summary>
        private GameObject CreateFallbackMainCanvas()
        {
            return UICanvasFallbackFactory.CreateMainCanvas(uiInputActions);
        }
    }
}
