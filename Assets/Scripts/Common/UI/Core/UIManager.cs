using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace Common.UI
{
    /// <summary>
    /// UI 관리 싱글톤
    /// 모든 UI의 생성, 표시, 숨김, 제거를 중앙에서 관리합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        // MainCanvas Addressable Address
        private const string MAIN_CANVAS_ADDRESS = "MainCanvas";

        // MainCanvas 핸들 (메모리 관리용)
        private AsyncOperationHandle<GameObject> mainCanvasHandle;

        /// <summary>
        /// 싱글톤 인스턴스 (동기 초기화, Fallback 모드)
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

        /// <summary>
        /// 비동기 초기화와 함께 인스턴스를 가져옵니다. (권장)
        /// </summary>
        public static async UniTask<UIManager> GetInstanceAsync(CancellationToken ct = default)
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("UIManager");
                instance = obj.AddComponent<UIManager>();
                DontDestroyOnLoad(obj);

                // 비동기 초기화
                await instance.InitializeAsync(ct);
            }

            return instance;
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
        /// 비동기 초기화 (권장)
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            if (isInitialized)
            {
                return;
            }

            // MainCanvas 찾기 또는 로드
            GameObject mainCanvasObj = await FindOrCreateMainCanvasAsync(ct);

            // UICanvas 초기화
            uiCanvas = new UICanvas(transform);
            uiCanvas.Initialize(mainCanvasObj);

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

            Debug.Log("UIManager initialized asynchronously");
        }

        /// <summary>
        /// 동기 초기화 (하위 호환용, Fallback 생성만 가능)
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            Debug.LogWarning("Synchronous Initialize() is deprecated. Use InitializeAsync() instead.");

            // 씬에서 찾거나 Fallback 생성
            GameObject mainCanvasObj = FindExistingOrCreateFallback();

            // UICanvas 초기화
            uiCanvas = new UICanvas(transform);
            uiCanvas.Initialize(mainCanvasObj);

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

            Debug.Log("UIManager initialized synchronously (fallback mode)");
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
        /// UIPath Attribute에서 Addressable Address를 가져옵니다.
        /// </summary>
        private string GetUIPath<T>() where T : UIBase
        {
            Type type = typeof(T);
            UIPathAttribute attribute = (UIPathAttribute)Attribute.GetCustomAttribute(type, typeof(UIPathAttribute));
            return attribute?.AddressableName;
        }

        /// <summary>
        /// 특정 레이어에 활성화된 UI가 있는지 확인합니다.
        /// </summary>
        private bool HasActiveUIInLayer(UILayer layer)
        {
            return activeUIs.Values.Any(ui => ui.Layer == layer);
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
                existing.transform.SetParent(transform);
                return existing;
            }

            // 2. Addressable에서 로드
            try
            {
                mainCanvasHandle = Addressables.InstantiateAsync(MAIN_CANVAS_ADDRESS, transform);
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
                existing.transform.SetParent(transform);
                return existing;
            }

            // Fallback 생성
            Debug.LogWarning("Creating fallback MainCanvas...");
            return CreateFallbackMainCanvas();
        }

        /// <summary>
        /// Fallback MainCanvas 생성 (Addressable 로드 실패 시)
        /// </summary>
        private GameObject CreateFallbackMainCanvas()
        {
            GameObject mainCanvasObj = new GameObject("MainCanvas");
            mainCanvasObj.transform.SetParent(transform);

            // Main Canvas 설정
            Canvas mainCanvas = mainCanvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = mainCanvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            mainCanvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem 생성 (이미 존재하지 않는 경우)
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(mainCanvasObj.transform);
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 레이어별 Nested Canvas 생성
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                GameObject layerObj = new GameObject(layer.ToString());
                layerObj.transform.SetParent(mainCanvasObj.transform);

                // RectTransform 설정 (Full Screen)
                RectTransform rect = layerObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // Nested Canvas 설정
                Canvas layerCanvas = layerObj.AddComponent<Canvas>();
                layerCanvas.overrideSorting = true;
                layerCanvas.sortingOrder = (int)layer;

                // GraphicRaycaster 추가
                layerObj.AddComponent<GraphicRaycaster>();
            }

            return mainCanvasObj;
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

                // Addressable 핸들 해제
                if (mainCanvasHandle.IsValid())
                {
                    Addressables.ReleaseInstance(mainCanvasHandle);
                }

                instance = null;
            }
        }
    }
}
