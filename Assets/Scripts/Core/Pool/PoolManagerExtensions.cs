using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// PoolManager의 편의성을 높이는 확장 메서드 모음
    /// </summary>
    public static class PoolManagerExtensions
    {
        /// <summary>
        /// MonoBehaviour에서 풀로부터 인스턴스를 가져옵니다.
        /// CancellationToken은 MonoBehaviour의 생명주기와 자동 연동됩니다.
        /// </summary>
        /// <typeparam name="T">가져올 Component 타입</typeparam>
        /// <param name="mb">호출하는 MonoBehaviour</param>
        /// <returns>Component 인스턴스</returns>
        public static async UniTask<T> GetFromPool<T>(this MonoBehaviour mb) where T : Component
        {
            CancellationToken ct = mb.GetCancellationTokenOnDestroy();
            return await PoolManager.Get<T>(ct);
        }

        /// <summary>
        /// MonoBehaviour에서 풀로부터 인스턴스를 가져옵니다.
        /// CancellationToken을 명시적으로 지정할 수 있습니다.
        /// </summary>
        /// <typeparam name="T">가져올 Component 타입</typeparam>
        /// <param name="mb">호출하는 MonoBehaviour</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>Component 인스턴스</returns>
        public static async UniTask<T> GetFromPool<T>(this MonoBehaviour mb, CancellationToken ct) where T : Component
        {
            return await PoolManager.Get<T>(ct);
        }

        /// <summary>
        /// 인스턴스를 풀로 반환합니다.
        /// </summary>
        /// <typeparam name="T">반환할 Component 타입</typeparam>
        /// <param name="instance">반환할 인스턴스</param>
        public static void ReturnToPool<T>(this T instance) where T : Component
        {
            PoolManager.Return(instance);
        }

        /// <summary>
        /// MonoBehaviour에서 특정 타입의 인스턴스를 프리로드합니다.
        /// CancellationToken은 MonoBehaviour의 생명주기와 자동 연동됩니다.
        /// </summary>
        /// <typeparam name="T">프리로드할 Component 타입</typeparam>
        /// <param name="mb">호출하는 MonoBehaviour</param>
        /// <param name="count">프리로드할 개수</param>
        public static async UniTask PreloadPool<T>(this MonoBehaviour mb, int count) where T : Component
        {
            CancellationToken ct = mb.GetCancellationTokenOnDestroy();
            await PoolManager.Preload<T>(count, ct);
        }

        /// <summary>
        /// MonoBehaviour에서 특정 타입의 인스턴스를 프리로드합니다.
        /// CancellationToken을 명시적으로 지정할 수 있습니다.
        /// </summary>
        /// <typeparam name="T">프리로드할 Component 타입</typeparam>
        /// <param name="mb">호출하는 MonoBehaviour</param>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public static async UniTask PreloadPool<T>(this MonoBehaviour mb, int count, CancellationToken ct) where T : Component
        {
            await PoolManager.Preload<T>(count, ct);
        }
    }
}
