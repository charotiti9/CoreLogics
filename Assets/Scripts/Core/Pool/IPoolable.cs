namespace Core.Pool
{
    /// <summary>
    /// 풀링 가능한 객체가 구현해야 하는 인터페이스
    /// 풀에서 가져올 때와 반환할 때의 생명주기를 정의합니다.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 풀에서 객체를 가져올 때 호출됩니다.
        /// 객체 초기화 로직을 여기에 작성합니다.
        /// </summary>
        void OnGetFromPool();

        /// <summary>
        /// 풀로 객체를 반환할 때 호출됩니다.
        /// 객체 정리 로직을 여기에 작성합니다.
        /// </summary>
        void OnReturnToPool();
    }
}
