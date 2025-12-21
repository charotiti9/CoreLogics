using System;

namespace Common.UI
{
    /// <summary>
    /// UI 설정을 정의하는 Attribute
    /// UIManager가 이 정보를 사용하여 자동으로 UI를 로드하고 설정합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIAttribute : Attribute
    {
        /// <summary>
        /// Addressable 주소
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// UI가 속한 레이어
        /// </summary>
        public UILayer Layer { get; }

        /// <summary>
        /// Dim 사용 여부
        /// true: Show 시 자동으로 Dim 표시, Hide 시 자동으로 Dim 숨김
        /// false: Dim 미사용 (기본값)
        /// </summary>
        public bool UseDim { get; }

        /// <summary>
        /// 씬 변경 시 자동으로 제거할지 여부
        /// true: 씬 변경 시 자동 제거 (기본값)
        /// false: 씬 전환 후에도 유지 (예: HUD, 로딩 UI)
        /// </summary>
        public bool DestroyOnSceneChange { get; }

        /// <summary>
        /// UIAttribute 생성자
        /// </summary>
        /// <param name="address">Addressable 주소</param>
        /// <param name="layer">UI 레이어 (필수)</param>
        /// <param name="useDim">Dim 사용 여부 (기본값: false)</param>
        /// <param name="destroyOnSceneChange">씬 변경 시 제거 여부 (기본값: true)</param>
        public UIAttribute(string address, UILayer layer, bool useDim = false, bool destroyOnSceneChange = true)
        {
            Address = address;
            Layer = layer;
            UseDim = useDim;
            DestroyOnSceneChange = destroyOnSceneChange;
        }
    }

    /// <summary>
    /// [Obsolete] 하위 호환성을 위한 Attribute
    /// UIAttribute를 사용하세요.
    /// </summary>
    [Obsolete("UIAddressAttribute는 더 이상 사용되지 않습니다. UIAttribute를 사용하세요.")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIAddressAttribute : Attribute
    {
        public string Address { get; }

        public UIAddressAttribute(string address)
        {
            Address = address;
        }
    }
}
