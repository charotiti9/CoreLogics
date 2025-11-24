namespace Core.Pool
{
    /// <summary>
    /// 풀링 시스템 설정
    /// </summary>
    public static class PoolConfig
    {
        /// <summary>
        /// 타입당 기본 최대 풀 크기
        /// 풀에 이 개수 이상의 인스턴스가 쌓이면 추가 반환되는 인스턴스는 파괴됩니다.
        /// </summary>
        public const int DEFAULT_MAX_POOL_SIZE = 10;

        /// <summary>
        /// 기본 초기 풀 크기 (프리로드)
        /// 0이면 프리로드하지 않습니다.
        /// </summary>
        public const int DEFAULT_INITIAL_POOL_SIZE = 0;
    }
}
