using System;

namespace Core.Pool
{
    /// <summary>
    /// Component 타입과 리소스 Address, Parent 이름을 매핑하는 Attribute
    /// PoolManager가 이 정보를 사용하여 자동으로 풀링을 처리합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class PoolAddressAttribute : Attribute
    {
        public string Address { get; }
        public string ParentName { get; }
        public bool UsePoolContainer { get; }
        public bool DontDestroyOnLoad { get; }

        /// <summary>
        /// PoolAddressAttribute 생성자 (Pool Container 사용)
        /// </summary>
        /// <param name="address">리소스 Address (Addressable 주소 또는 Resources 경로)</param>
        /// <param name="dontDestroyOnLoad">씬 전환 시에도 유지할지 여부 (기본값: false)</param>
        public PoolAddressAttribute(string address, bool dontDestroyOnLoad = false)
        {
            Address = address;
            ParentName = null;
            UsePoolContainer = true;
            DontDestroyOnLoad = dontDestroyOnLoad;
        }

        /// <summary>
        /// PoolAddressAttribute 생성자 (사용자 지정 부모 사용)
        /// </summary>
        /// <param name="address">리소스 Address (Addressable 주소 또는 Resources 경로)</param>
        /// <param name="parentName">부모 GameObject의 이름 (없으면 자동 생성)</param>
        /// <param name="dontDestroyOnLoad">씬 전환 시에도 유지할지 여부 (기본값: false)</param>
        public PoolAddressAttribute(string address, string parentName, bool dontDestroyOnLoad = false)
        {
            Address = address;
            ParentName = parentName;
            UsePoolContainer = false;
            DontDestroyOnLoad = dontDestroyOnLoad;
        }
    }
}
