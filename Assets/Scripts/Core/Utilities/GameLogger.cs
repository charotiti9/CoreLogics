using UnityEngine;

namespace Core.Utilities
{
    /// <summary>
    /// 게임 전역에서 사용하는 범용 로거
    /// 조건부 로그 출력으로 성능 최적화
    /// </summary>
    public static class GameLogger
    {
        /// <summary>
        /// 로그 출력 활성화 여부
        /// Editor와 Development Build에서만 활성화
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Info 레벨 로그
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Warning 레벨 로그 (항상 출력)
        /// </summary>
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Error 레벨 로그 (항상 출력)
        /// </summary>
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }
    }
}
