using System;
using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// 런타임 해상도 변경 감지 및 전파
    /// GameFlowManager의 IUpdatable 인터페이스를 통해 중앙 관리됩니다.
    /// Application focus 이벤트와 연동하여 효율적으로 동작합니다.
    /// MonoBehaviour를 사용하지 않는 순수 C# 클래스입니다.
    /// </summary>
    public class UIResolutionHandler : IUpdatable
    {
        private static UIResolutionHandler instance;
        private static Vector2Int currentResolution;

        // 해상도 체크 간격 (에디터: 0.2초, 빌드: 1초)
        #if UNITY_EDITOR
        private const float CHECK_INTERVAL = 0.2f;
        #else
        private const float CHECK_INTERVAL = 1f;
        #endif

        private float elapsedTime = 0f;
        private bool isApplicationFocused = true;

        /// <summary>
        /// 해상도 변경 이벤트 (인스턴스 이벤트)
        /// Dispose() 시 자동으로 정리됩니다.
        /// </summary>
        public event Action<Vector2Int> OnResolutionChangedEvent;

        /// <summary>
        /// 해상도 변경 이벤트 (static 래퍼, 하위 호환성)
        /// 구독자는 반드시 명시적으로 해제해야 합니다.
        /// </summary>
        public static event Action<Vector2Int> OnResolutionChanged
        {
            add
            {
                if (instance != null)
                {
                    instance.OnResolutionChangedEvent += value;
                }
            }
            remove
            {
                if (instance != null)
                {
                    instance.OnResolutionChangedEvent -= value;
                }
            }
        }

        /// <summary>
        /// 현재 해상도
        /// </summary>
        public static Vector2Int CurrentResolution => currentResolution;

        /// <summary>
        /// 싱글톤 인스턴스가 살아있는지 확인
        /// </summary>
        public static bool IsAlive() => instance != null;

        /// <summary>
        /// Update 실행 우선순위 (낮을수록 먼저 실행)
        /// </summary>
        public int UpdateOrder => 0;

        /// <summary>
        /// UIResolutionHandler 인스턴스를 생성하고 GameFlowManager에 등록합니다.
        /// </summary>
        public static void Initialize()
        {
            if (instance != null)
            {
                return; // 이미 생성됨
            }

            instance = new UIResolutionHandler();

            // 현재 해상도 초기화
            currentResolution = new Vector2Int(Screen.width, Screen.height);

            // Application focus 이벤트 등록
            Application.focusChanged += instance.OnApplicationFocusChanged;

            // GameFlowManager에 등록
            if (GameFlowManager.IsAlive())
            {
                GameFlowManager.Instance.RegisterUpdatable(instance);
            }
            else
            {
                Debug.LogWarning("[UIResolutionHandler] GameFlowManager가 없습니다. 해상도 감지가 동작하지 않습니다.");
            }
        }

        /// <summary>
        /// Application focus 변경 시 호출됩니다.
        /// 포커스를 다시 얻었을 때 즉시 해상도를 체크합니다.
        /// </summary>
        private void OnApplicationFocusChanged(bool hasFocus)
        {
            isApplicationFocused = hasFocus;

            // 포커스를 다시 얻었을 때 즉시 체크
            if (hasFocus)
            {
                CheckResolutionChange();
            }
        }

        /// <summary>
        /// GameFlowManager에서 호출되는 업데이트 메서드
        /// 일정 간격으로 해상도 변경을 확인합니다. (에디터: 0.2초, 빌드: 1초)
        /// 포커스가 없을 때는 체크하지 않아 성능을 절약합니다.
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            // 포커스가 없으면 체크하지 않음
            if (!isApplicationFocused)
            {
                return;
            }

            elapsedTime += deltaTime;

            if (elapsedTime >= CHECK_INTERVAL)
            {
                elapsedTime = 0f;
                CheckResolutionChange();
            }
        }

        /// <summary>
        /// 해상도 변경 여부를 확인하고 이벤트를 발생시킵니다.
        /// </summary>
        private void CheckResolutionChange()
        {
            Vector2Int newResolution = new Vector2Int(Screen.width, Screen.height);

            // 해상도가 변경되었는지 확인
            if (newResolution != currentResolution)
            {
                currentResolution = newResolution;

                // 인스턴스 이벤트 발생
                OnResolutionChangedEvent?.Invoke(currentResolution);

                Debug.Log($"해상도 변경 감지: {currentResolution.x}x{currentResolution.y}");
            }
        }

        /// <summary>
        /// IDisposable 구현: GameFlowManager에서 등록 해제 및 이벤트 정리
        /// </summary>
        public void Dispose()
        {
            if (instance == this)
            {
                // Application focus 이벤트 해제
                Application.focusChanged -= OnApplicationFocusChanged;

                // GameFlowManager에서 등록 해제
                if (GameFlowManager.IsAlive())
                {
                    GameFlowManager.Instance.UnregisterUpdatable(this);
                }

                // 모든 이벤트 구독자 해제 (메모리 누수 방지)
                OnResolutionChangedEvent = null;

                instance = null;
            }
        }
    }
}
