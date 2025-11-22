using System;

namespace Common.UI
{
    /// <summary>
    /// UI 프리팹의 Addressable 경로를 지정하는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UIPathAttribute : Attribute
    {
        /// <summary>
        /// Addressable 경로
        /// </summary>
        public string AddressablePath { get; }

        /// <summary>
        /// UI 프리팹의 Addressable 경로를 지정합니다.
        /// </summary>
        /// <param name="addressablePath">Addressable 경로 (예: "UI/PopUp/SettingsPopup")</param>
        public UIPathAttribute(string addressablePath)
        {
            AddressablePath = addressablePath;
        }
    }
}
