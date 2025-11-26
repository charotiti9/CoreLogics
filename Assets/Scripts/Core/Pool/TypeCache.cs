using System;

namespace Core.Pool
{
    /// <summary>
    /// 제네릭 타입 캐싱 유틸리티
    /// typeof(T) 호출 비용을 줄이기 위한 정적 캐싱
    /// </summary>
    public static class TypeCache<T>
    {
        public static readonly Type Type = typeof(T);
    }
}
