using System;
using UnityEngine;

namespace Common.UI
{
    /// <summary>
    /// 런타임 해상도 변경 감지 및 전파
    /// UIManager가 초기화 시 자동으로 생성합니다.
    /// </summary>
    public class UIResolutionHandler : MonoBehaviour
    {
        private static UIResolutionHandler instance;
        private static Vector2Int currentResolution;

        /// <summary>
        /// 해상도 변경 이벤트
        /// </summary>
        public static event Action<Vector2Int> OnResolutionChanged;

        /// <summary>
        /// 현재 해상도
        /// </summary>
        public static Vector2Int CurrentResolution => currentResolution;

        /// <summary>
        /// UIResolutionHandler 인스턴스를 생성합니다.
        /// </summary>
        public static void Initialize(Transform parent)
        {
            if (instance != null)
            {
                return; // 이미 생성됨
            }

            GameObject obj = new GameObject("UIResolutionHandler");
            obj.transform.SetParent(parent);
            instance = obj.AddComponent<UIResolutionHandler>();

            // 현재 해상도 초기화
            currentResolution = new Vector2Int(Screen.width, Screen.height);
        }

        private void Update()
        {
            CheckResolutionChange();
        }

        /// <summary>
        /// 해상도 변경 여부를 확인하고 이벤트를 발생시킵니다.
        /// </summary>
        public static void CheckResolutionChange()
        {
            Vector2Int newResolution = new Vector2Int(Screen.width, Screen.height);

            // 해상도가 변경되었는지 확인
            if (newResolution != currentResolution)
            {
                currentResolution = newResolution;

                // 이벤트 발생
                OnResolutionChanged?.Invoke(currentResolution);

                Debug.Log($"해상도 변경 감지: {currentResolution.x}x{currentResolution.y}");
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                OnResolutionChanged = null;
            }
        }
    }
}
