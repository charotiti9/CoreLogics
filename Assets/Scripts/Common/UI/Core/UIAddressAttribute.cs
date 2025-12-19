using System;

namespace Common.UI
{
    /// <summary>
    /// UI 타입과 Addressable 주소를 매핑하는 Attribute
    /// UIManager가 이 정보를 사용하여 자동으로 UI를 로드합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIAddressAttribute : Attribute
    {
        public string Address { get; }

        /// <summary>
        /// UIAddressAttribute 생성자
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        public UIAddressAttribute(string address)
        {
            Address = address;
        }
    }
}
