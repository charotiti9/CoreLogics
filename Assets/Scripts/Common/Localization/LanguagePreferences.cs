using UnityEngine;

/// <summary>
/// 언어 설정 관리자
/// PlayerPrefs 저장/로드 및 시스템 언어 감지 전담
/// </summary>
public class LanguagePreferences
{
    private const string LANGUAGE_PREFS_KEY = "Localization_Language";

    private LanguageType currentLanguage;

    /// <summary>
    /// 현재 설정된 언어
    /// </summary>
    public LanguageType CurrentLanguage => currentLanguage;

    /// <summary>
    /// 생성자
    /// </summary>
    public LanguagePreferences()
    {
        currentLanguage = LanguageType.Korean; // 기본값
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 언어 설정 불러오기
    /// 저장된 값이 없으면 시스템 언어 감지
    /// </summary>
    public void LoadLanguage()
    {
        if (PlayerPrefs.HasKey(LANGUAGE_PREFS_KEY))
        {
            int savedLanguage = PlayerPrefs.GetInt(LANGUAGE_PREFS_KEY);
            currentLanguage = (LanguageType)savedLanguage;
            Debug.Log($"[LanguagePreferences] 저장된 언어 로드: {currentLanguage}");
        }
        else
        {
            // 저장된 언어가 없으면 시스템 언어 감지
            currentLanguage = DetectSystemLanguage();
            Debug.Log($"[LanguagePreferences] 시스템 언어 감지: {currentLanguage}");
        }
    }

    /// <summary>
    /// 언어 변경 및 PlayerPrefs에 저장
    /// </summary>
    public void SetLanguage(LanguageType language)
    {
        if (currentLanguage == language)
            return;

        currentLanguage = language;

        // PlayerPrefs에 저장
        PlayerPrefs.SetInt(LANGUAGE_PREFS_KEY, (int)language);
        PlayerPrefs.Save();

        Debug.Log($"[LanguagePreferences] 언어 변경 및 저장됨: {language}");
    }

    /// <summary>
    /// 시스템 언어 감지
    /// </summary>
    private LanguageType DetectSystemLanguage()
    {
        SystemLanguage systemLang = Application.systemLanguage;

        switch (systemLang)
        {
            case SystemLanguage.Korean:
                return LanguageType.Korean;

            case SystemLanguage.English:
                return LanguageType.English;

            // 기타 언어는 영어로 폴백
            default:
                return LanguageType.English;
        }
    }
}
