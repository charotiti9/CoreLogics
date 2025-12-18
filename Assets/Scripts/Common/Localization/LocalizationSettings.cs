using UnityEngine;
using TMPro;

/// <summary>
/// 로컬라이징 폰트 설정
/// ScriptableObject로 관리하여 에디터에서 설정 가능
/// </summary>
[CreateAssetMenu(fileName = "LocalizationSettings", menuName = "Game/LocalizationSettings")]
public class LocalizationSettings : ScriptableObject
{
    [Header("Language Fonts")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    /// <summary>
    /// 언어별 폰트 반환
    /// </summary>
    public TMP_FontAsset GetFont(LanguageType language)
    {
        switch (language)
        {
            case LanguageType.Korean:
                return koreanFont;

            case LanguageType.English:
                return englishFont;

            default:
                return null;
        }
    }
}
