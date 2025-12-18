using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 언어와 폰트를 매핑하는 클래스
/// </summary>
[Serializable]
public class LanguageFontPair
{
    public LanguageType Language;
    public TMP_FontAsset Font;
}

/// <summary>
/// 로컬라이징 폰트 설정
/// ScriptableObject로 관리하여 에디터에서 설정 가능
/// </summary>
[CreateAssetMenu(fileName = "LocalizationSettings", menuName = "Game/LocalizationSettings")]
public class LocalizationSettings : ScriptableObject
{
    [Header("Language Fonts")]
    [SerializeField] private List<LanguageFontPair> languageFonts = new List<LanguageFontPair>();

    private Dictionary<LanguageType, TMP_FontAsset> fontDict;

    private void OnEnable()
    {
        BuildFontDictionary();
    }

    /// <summary>
    /// 폰트 Dictionary 빌드
    /// </summary>
    private void BuildFontDictionary()
    {
        fontDict = new Dictionary<LanguageType, TMP_FontAsset>();

        foreach (var pair in languageFonts)
        {
            if (pair.Font != null)
            {
                fontDict[pair.Language] = pair.Font;
            }
        }
    }

    /// <summary>
    /// 언어별 폰트 반환
    /// </summary>
    public TMP_FontAsset GetFont(LanguageType language)
    {
        if (fontDict == null)
        {
            BuildFontDictionary();
        }

        if (fontDict.TryGetValue(language, out var font))
        {
            return font;
        }

        return null;
    }
}
