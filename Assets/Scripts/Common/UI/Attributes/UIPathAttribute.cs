using System;
using System.IO;

namespace Common.UI
{
    /// <summary>
    /// UI 프리팹 경로를 지정하는 Attribute
    /// 경로에서 파일 이름을 추출하여 Addressable Address로 사용합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class UIPathAttribute : Attribute
    {
        /// <summary>
        /// 프리팹 경로 (전체 경로)
        /// </summary>
        public string PrefabPath { get; }

        /// <summary>
        /// Addressable Address (경로에서 파일명만 추출)
        /// </summary>
        public string AddressableName { get; }

        /// <summary>
        /// UI 프리팹 경로를 지정합니다.
        /// Addressable Groups에 추가 시 자동으로 설정되는 Address(파일명)를 사용합니다.
        /// </summary>
        /// <param name="prefabPath">프리팹 경로 (예: "UI/Examples/TestPopup")</param>
        public UIPathAttribute(string prefabPath)
        {
            PrefabPath = prefabPath;
            // 경로에서 파일명만 추출 (Addressable의 기본 Address)
            AddressableName = Path.GetFileNameWithoutExtension(prefabPath);
        }
    }
}
