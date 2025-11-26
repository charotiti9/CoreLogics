using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Core.Pool;
using Core.Addressable;

namespace Common.UI
{
    /// <summary>
    /// UI 관리 싱글톤
    /// 모든 UI의 생성, 표시, 숨김, 제거를 중앙에서 관리합니다.
    /// LazyMonoSingleton을 사용하여 자동 생성 및 관리됩니다.
    /// </summary>
    public partial class UIManager : LazyMonoSingleton<UIManager>
    {
        // MainCanvas Addressable Address
        private const string MAIN_CANVAS_ADDRESS = "MainCanvas";

        // MainCanvas 핸들 (메모리 관리용)
        private AsyncOperationHandle<GameObject> mainCanvasHandle;

        /// <summary>
        /// UI Input Actions Asset
        /// Inspector에서 할당하거나, 없으면 경고가 출력됩니다.
        /// </summary>
        [SerializeField]
        private InputActionAsset uiInputActions;

        private UICanvas uiCanvas;
        private UIDimController dimController;
        private UIStack uiStack;

        // 활성화된 UI 관리 (타입별)
        private readonly Dictionary<Type, UIBase> activeUIs = new Dictionary<Type, UIBase>();

        // 초기화 완료 여부
        private bool isInitialized = false;

        /// <summary>
        /// UI를 표시합니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="layer">UI 레이어</param>
        /// <param name="data">초기화 데이터</param>
        /// <param name="useDim">Dim 사용 여부</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>생성된 UI 인스턴스</returns>
        public async UniTask<T> ShowAsync<T>(UILayer layer, object data = null, bool useDim = false, CancellationToken ct = default) where T : UIBase
        {
            Type type = typeof(T);

            // 이미 표시 중인 UI가 있으면 반환
            if (activeUIs.TryGetValue(type, out UIBase existingUI) && existingUI != null)
            {
                Debug.LogWarning($"UI {type.Name} is already showing!");
                return existingUI as T;
            }

            try
            {
                // 입력 차단
                UIInputBlocker.Instance.Block();

                // PoolManager를 통해 UI 인스턴스 획득
                T ui = await PoolManager.GetFromPool<T>(ct);

                if (ui == null)
                {
                    Debug.LogError($"Failed to load UI: {type.Name}");
                    return null;
                }

                // UI를 올바른 Canvas Layer로 이동
                Transform canvasLayer = uiCanvas.GetCanvasTransform(layer);
                ui.transform.SetParent(canvasLayer, false);

                // 활성 UI로 등록
                activeUIs[type] = ui;
                ui.ParentCanvas = uiCanvas.GetCanvas(layer);

                // Dim 표시 (UI Stack 지원)
                if (useDim)
                {
                    await dimController.ShowDimAsync(ui, layer, 0.7f, ct);
                }

                // UI 표시
                await ui.ShowInternalAsync(data, ct);

                // 스택에 추가 (PopUp 레이어만)
                if (layer == UILayer.PopUp)
                {
                    uiStack.Push(ui);
                }

                return ui;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error showing UI {type.Name}: {ex.Message}");
                return null;
            }
            finally
            {
                // 입력 차단 해제
                UIInputBlocker.Instance.Unblock();
            }
        }

        /// <summary>
        /// UI를 숨깁니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="immediate">즉시 숨김 여부 (애니메이션 스킵)</param>
        public void Hide<T>(bool immediate = false) where T : UIBase
        {
            Type type = typeof(T);

            if (!activeUIs.TryGetValue(type, out UIBase ui) || ui == null)
            {
                return; // 표시 중이 아님
            }

            HideUIAsync(ui, immediate, CancellationToken.None).Forget();
        }

        /// <summary>
        /// 특정 레이어의 모든 UI를 숨깁니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="immediate">즉시 숨김 여부</param>
        public void HideAll(UILayer layer, bool immediate = false)
        {
            List<UIBase> uisInLayer = new List<UIBase>();
            foreach (var ui in activeUIs.Values)
            {
                if (ui.Layer == layer)
                {
                    uisInLayer.Add(ui);
                }
            }

            foreach (var ui in uisInLayer)
            {
                HideUIAsync(ui, immediate, CancellationToken.None).Forget();
            }
        }

        /// <summary>
        /// 현재 표시 중인 UI를 가져옵니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <returns>UI 인스턴스 (없으면 null)</returns>
        public T Get<T>() where T : UIBase
        {
            Type type = typeof(T);

            if (activeUIs.TryGetValue(type, out UIBase ui))
            {
                // null 참조 체크 (UI가 직접 파괴된 경우)
                if (ui == null)
                {
                    activeUIs.Remove(type);
                    Debug.LogWarning($"[UIManager] {type.Name}이(가) Dictionary에 null로 남아있어 제거했습니다.");
                    return null;
                }
                return ui as T;
            }

            return null;
        }

        /// <summary>
        /// UI가 표시 중인지 확인합니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <returns>표시 중이면 true</returns>
        public bool IsShowing<T>() where T : UIBase
        {
            return activeUIs.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 뒤로가기 처리 (스택 최상단 UI 닫기)
        /// </summary>
        public void HandleBackKey()
        {
            UIBase ui = uiStack.Pop();
            if (ui != null)
            {
                HideUIAsync(ui, false, CancellationToken.None).Forget();
            }
        }

        /// <summary>
        /// 특정 레이어의 Canvas를 가져옵니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <returns>Canvas</returns>
        public Canvas GetCanvas(UILayer layer)
        {
            return uiCanvas.GetCanvas(layer);
        }
    }
}
