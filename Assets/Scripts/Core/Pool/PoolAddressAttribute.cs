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

        /// <summary>
        /// PoolAddressAttribute 생성자
        /// </summary>
        /// <param name="address">리소스 Address (Addressable 주소 또는 Resources 경로)</param>
        /// <param name="parentName">부모 GameObject의 이름 (선택사항, 비어있으면 Pool Container 사용, 없으면 자동 생성)</param>
        public PoolAddressAttribute(string address, string parentName = null)
        {
            Address = address;
            ParentName = parentName;
        }
    }
}
