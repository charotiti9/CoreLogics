using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Common.UI
{
    /// <summary>
    /// UI 관리 싱글톤
    /// 모든 UI의 생성, 표시, 숨김, 제거를 중앙에서 관리합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        /// <summary>
        /// 싱글톤 인스턴스
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("UIManager");
                    instance = obj.AddComponent<UIManager>();
                    DontDestroyOnLoad(obj);
                    instance.Initialize();
                }
                return instance;
            }
        }

        private UICanvas uiCanvas;
        private UIPool uiPool;
        private UIDimController dimController;
        private UIStack uiStack;

        // 활성화된 UI 관리 (타입별)
        private readonly Dictionary<Type, UIBase> activeUIs = new Dictionary<Type, UIBase>();

        // 초기화 완료 여부
        private bool isInitialized = false;

        /// <summary>
        /// 초기화
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            // UICanvas 초기화
            uiCanvas = new UICanvas(transform);
            uiCanvas.Initialize();

            // UIPool 초기화
            uiPool = new UIPool();

            // UIDimController 초기화
            dimController = new UIDimController(uiCanvas);

            // UIStack 초기화
            uiStack = new UIStack();

            // UIResolutionHandler 초기화
            UIResolutionHandler.Initialize(transform);
            UIResolutionHandler.OnResolutionChanged += OnResolutionChanged;

            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;

            isInitialized = true;

            Debug.Log("UIManager initialized");
        }

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
                UIInputBlocker.Block();

                // Addressable 경로 가져오기
                string path = GetUIPath<T>();
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogError($"UIPath attribute not found for {type.Name}");
                    return null;
                }

                // 풀에서 가져오거나 로드
                Transform parent = uiCanvas.GetCanvasTransform(layer);
                T ui = await uiPool.GetAsync<T>(path, parent, ct);

                if (ui == null)
                {
                    Debug.LogError($"Failed to load UI: {type.Name}");
                    return null;
                }

                // 활성 UI로 등록
                activeUIs[type] = ui;
                ui.ParentCanvas = uiCanvas.GetCanvas(layer);

                // Dim 표시
                if (useDim)
                {
                    await dimController.ShowDimAsync(layer, 0.7f, ct);
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
                UIInputBlocker.Unblock();
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
        /// UI를 숨깁니다. (내부 비동기 처리)
        /// </summary>
        private async UniTaskVoid HideUIAsync(UIBase ui, bool immediate, CancellationToken ct)
        {
            try
            {
                UIInputBlocker.Block();

                UILayer layer = ui.Layer;
                Type type = ui.GetType();

                // 스택에서 제거
                uiStack.Remove(ui);

                // UI 숨김
                await ui.HideInternalAsync(immediate, ct);

                // 활성 UI에서 제거
                activeUIs.Remove(type);

                // 풀로 반환
                uiPool.Return(ui);

                // Dim 숨김 (해당 레이어에 다른 UI가 없으면)
                if (!HasActiveUIInLayer(layer))
                {
                    if (immediate)
                    {
                        dimController.ClearDim(layer);
                    }
                    else
                    {
                        await dimController.HideDimAsync(layer, ct);
                    }
                }
            }
            finally
            {
                UIInputBlocker.Unblock();
            }
        }

        /// <summary>
        /// 특정 레이어의 모든 UI를 숨깁니다.
        /// </summary>
        /// <param name="layer">레이어</param>
        /// <param name="immediate">즉시 숨김 여부</param>
        public void HideAll(UILayer layer, bool immediate = false)
        {
            List<UIBase> uisInLayer = activeUIs.Values.Where(ui => ui.Layer == layer).ToList();

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

        /// <summary>
        /// 씬 로드 시 호출됩니다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // DestroyOnSceneChange가 true인 UI만 제거
            List<UIBase> uisToRemove = activeUIs.Values.Where(ui => ui.DestroyOnSceneChange).ToList();

            foreach (var ui in uisToRemove)
            {
                HideUIAsync(ui, true, CancellationToken.None).Forget();
            }

            Debug.Log($"Scene loaded: {scene.name}, removed {uisToRemove.Count} UIs");
        }

        /// <summary>
        /// 해상도 변경 시 호출됩니다.
        /// </summary>
        private void OnResolutionChanged(Vector2Int newResolution)
        {
            // 모든 활성 UI에 이벤트 전파
            foreach (var ui in activeUIs.Values)
            {
                if (ui != null)
                {
                    ui.OnResolutionChanged(newResolution);
                }
            }

            Debug.Log($"Resolution changed: {newResolution}, notified {activeUIs.Count} UIs");
        }

        /// <summary>
        /// UIPath Attribute에서 경로를 가져옵니다.
        /// </summary>
        private string GetUIPath<T>() where T : UIBase
        {
            Type type = typeof(T);
            UIPathAttribute attribute = (UIPathAttribute)Attribute.GetCustomAttribute(type, typeof(UIPathAttribute));
            return attribute?.AddressablePath;
        }

        /// <summary>
        /// 특정 레이어에 활성화된 UI가 있는지 확인합니다.
        /// </summary>
        private bool HasActiveUIInLayer(UILayer layer)
        {
            return activeUIs.Values.Any(ui => ui.Layer == layer);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                // 이벤트 해제
                SceneManager.sceneLoaded -= OnSceneLoaded;
                UIResolutionHandler.OnResolutionChanged -= OnResolutionChanged;

                // 풀 정리
                uiPool?.Clear();

                // Dim 정리
                dimController?.ClearAll();

                // 스택 정리
                uiStack?.Clear();

                instance = null;
            }
        }
    }
}
