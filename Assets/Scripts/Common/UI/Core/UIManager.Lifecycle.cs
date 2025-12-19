using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Addressable;

namespace Common.UI
{
    /// <summary>
    /// UIManager의 생명주기 관리 파트
    /// </summary>
    public partial class UIManager
    {
        /// <summary>
        /// UIManager를 비동기로 초기화합니다.
        /// Initialize()에서 자동으로 호출되므로 직접 호출하지 마세요.
        /// </summary>
        private async UniTask InitializeAsync(CancellationToken ct = default)
        {
            if (isInitialized)
            {
                return;
            }

            // MainCanvas 찾기 또는 로드
            GameObject mainCanvasObj = await FindOrCreateMainCanvasAsync(ct);

            // 공통 초기화 실행
            InitializeInternal(mainCanvasObj);
        }

        /// <summary>
        /// 공통 초기화 로직
        /// </summary>
        private void InitializeInternal(GameObject mainCanvasObj)
        {
            // MainCanvas를 DontDestroyOnLoad로 설정
            DontDestroyOnLoad(mainCanvasObj);

            // EventSystem도 DontDestroyOnLoad로 설정
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                DontDestroyOnLoad(eventSystem.gameObject);
            }

            // UICanvas 초기화
            uiCanvas = new UICanvas();
            uiCanvas.Initialize(mainCanvasObj);

            // UIDimController 초기화
            dimController = new UIDimController(uiCanvas);

            // UIStack 초기화
            uiStack = new UIStack();

            // UIResolutionHandler 초기화 (GameFlowManager 통합)
            UIResolutionHandler.Initialize();
            UIResolutionHandler.OnResolutionChanged += OnResolutionChanged;

            // UIInputBlocker에 최상위 Canvas 주입
            UIInputBlocker.Instance.SetTargetCanvas(uiCanvas.GetCanvas(UILayer.Transition));

            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;

            isInitialized = true;
        }

        /// <summary>
        /// 씬 로드 시 호출됩니다.
        /// DestroyOnSceneChange == true인 UI를 Destroy합니다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // null 참조 정리 (먼저 수행)
            CleanupNullReferences();

            // DestroyOnSceneChange가 true인 UI만 Destroy
            List<UIBase> uisToDestroy = new List<UIBase>();
            foreach (var ui in spawnedUIs.Values)
            {
                if (ui != null && ui.DestroyOnSceneChange)
                {
                    uisToDestroy.Add(ui);
                }
            }

            foreach (var ui in uisToDestroy)
            {
                // Hide가 아닌 Destroy 호출
                DestroyUI(ui);
            }
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
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 명시적 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (UIResolutionHandler.IsAlive())
            {
                UIResolutionHandler.OnResolutionChanged -= OnResolutionChanged;
            }

            // UIResolutionHandler 정리
            if (UIResolutionHandler.IsAlive())
            {
                // Dispose 호출로 GameFlowManager에서 등록 해제
                // 주의: 이벤트는 구독자가 직접 해제했으므로 여기서는 건드리지 않음
            }

            // Dim 정리
            dimController?.ClearAll();

            // 스택 정리
            uiStack?.Clear();

            // 모든 Spawned UI Destroy 및 Addressable 핸들 해제
            List<UIBase> uisToDestroy = new List<UIBase>(spawnedUIs.Values);
            foreach (var ui in uisToDestroy)
            {
                if (ui != null)
                {
                    DestroyUI(ui);
                }
            }

            // MainCanvas Addressable 해제
            // AddressableLoader는 참조 카운팅을 사용하므로,
            // Addressable에서 로드한 경우에만 참조 카운트가 감소합니다.
            AddressableLoader.Instance.Release(MAIN_CANVAS_ADDRESS);
        }
    }
}
