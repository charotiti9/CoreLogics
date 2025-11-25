using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Core.Pool.PoolLogger;
using Core.Pool;

namespace Common.UI
{
    /// <summary>
    /// UI 인스턴스 풀링 (Addressable 기반)
    /// AddressablePool을 내부적으로 사용하여 UI를 풀링하고,
    /// UIBase의 생명주기를 자동으로 관리합니다.
    /// </summary>
    public class UIPool
    {
        // 내부적으로 AddressablePool 사용
        private readonly AddressablePool<UIBase> pool;

        /// <summary>
        /// UIPool 생성자
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        public UIPool(int defaultMaxSize = PoolConfig.DEFAULT_MAX_POOL_SIZE)
        {
            pool = new AddressablePool<UIBase>("UIPool", defaultMaxSize);
        }

        /// <summary>
        /// 특정 UI 타입의 최대 풀 크기를 설정합니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="maxSize">최대 풀 크기</param>
        public void SetPoolSize<T>(int maxSize) where T : UIBase
        {
            pool.SetPoolSize<T>(maxSize);
        }

        /// <summary>
        /// 풀에서 UI를 가져오거나 새로 생성합니다.
        /// UI 생명주기를 자동으로 관리합니다 (Initialize).
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="addressableName">Addressable Address (프리팹 이름)</param>
        /// <param name="parent">부모 Transform</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>UI 인스턴스</returns>
        public async UniTask<T> GetAsync<T>(string addressableName, Transform parent, CancellationToken ct) where T : UIBase
        {
            // AddressablePool에서 가져오기
            T ui = await pool.GetAsync<T>(addressableName, parent, ct);

            if (ui == null)
            {
                return null;
            }

            // UI 생명주기: 초기화 (최초 1회만)
            if (!ui.IsInitialized)
            {
                ui.OnInitialize(null);
            }

            Log($"[UIPool] UI 가져오기 성공: {typeof(T).Name}");
            return ui;
        }

        /// <summary>
        /// UI를 풀로 반환합니다.
        /// UI 생명주기를 자동으로 관리합니다 (Hide).
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="instance">반환할 UI 인스턴스</param>
        public async void Return<T>(T instance) where T : UIBase
        {
            if (instance == null)
            {
                return;
            }

            // UI 생명주기: Hide (즉시 숨김)
            if (instance.IsShowing)
            {
                try
                {
                    await instance.HideInternalAsync(immediate: true, CancellationToken.None);
                }
                catch (System.Exception ex)
                {
                    LogWarning($"[UIPool] Hide 중 예외 발생: {ex.Message}");
                }
            }

            // AddressablePool로 반환
            pool.Return(instance);

            Log($"[UIPool] UI 반환: {typeof(T).Name}");
        }

        /// <summary>
        /// 특정 UI 타입의 인스턴스를 미리 로드하여 풀에 채웁니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        /// <param name="addressableName">Addressable Address</param>
        /// <param name="count">프리로드할 개수</param>
        /// <param name="ct">CancellationToken</param>
        public async UniTask PreloadAsync<T>(string addressableName, int count, CancellationToken ct) where T : UIBase
        {
            await pool.PreloadAsync<T>(addressableName, count, ct);
        }

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// </summary>
        /// <typeparam name="T">UI 타입</typeparam>
        public void ClearType<T>() where T : UIBase
        {
            pool.ClearType<T>();
        }

        /// <summary>
        /// 모든 풀을 비웁니다.
        /// </summary>
        public void Clear()
        {
            pool.Clear();
        }

        /// <summary>
        /// 특정 타입의 풀 개수를 반환합니다.
        /// </summary>
        public int GetPoolCount<T>() where T : UIBase
        {
            return pool.GetPoolCount<T>();
        }

        /// <summary>
        /// 활성 인스턴스 개수를 반환합니다.
        /// </summary>
        public int GetActiveCount()
        {
            return pool.GetActiveCount();
        }

        /// <summary>
        /// 디버그 정보를 출력합니다.
        /// </summary>
        public void PrintDebugInfo()
        {
            pool.PrintDebugInfo();
        }

        /// <summary>
        /// 리소스를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            pool.Dispose();
        }
    }
}
