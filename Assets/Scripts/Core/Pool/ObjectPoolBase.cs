using UnityEngine;

namespace Core.Pool
{
    /// <summary>
    /// ObjectPool의 NonGeneric 베이스 클래스
    /// PoolManager에서 박싱 없이 다양한 타입의 풀을 관리하기 위한 공통 인터페이스를 제공합니다.
    /// IPoolable(사용자 컴포넌트용)과 구분하기 위해 Base 클래스로 명명했습니다.
    /// </summary>
    public abstract class ObjectPoolBase
    {
        /// <summary>
        /// 인스턴스를 풀로 반환합니다.
        /// </summary>
        /// <param name="instance">반환할 Component 인스턴스</param>
        public abstract void Return(Component instance);

        /// <summary>
        /// 풀의 모든 리소스를 정리합니다.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// 풀의 모든 리소스를 정리하고 Pool Container를 제거합니다.
        /// </summary>
        public abstract void Dispose();
    }
}
