using System;
using System.Threading;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

/// <summary>
/// 로컬라이징 시스템 관리자 (Facade)
/// 내부 컴포넌트들을 조합하여 간단한 API 제공
/// CSV 기반 다국어 지원
/// </summary>
public class LocalizationManager : EagerSingleton<LocalizationManager>
{
    // 내부 컴포넌트들
    private LocalizationDataProvider dataProvider;
    private LanguagePreferences languagePreferences;
    private LocalizationFontProvider fontProvider;

    private bool isInitialized;

    /// <summary>
    /// 현재 설정된 언어
    /// </summary>
    public LanguageType CurrentLanguage => languagePreferences.CurrentLanguage;

    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 언어 변경 이벤트
    /// LocalizedText 컴포넌트들이 구독하여 자동 갱신
    /// </summary>
    public event Action<LanguageType> OnLanguageChanged;

    /// <summary>
    /// EagerSingleton 초기화
    /// </summary>
    protected override void Initialize()
    {
        // 내부 컴포넌트 생성
        dataProvider = new LocalizationDataProvider();
        languagePreferences = new LanguagePreferences();
        fontProvider = new LocalizationFontProvider();

        isInitialized = false;
    }

    /// <summary>
    /// 초기화
    /// CSVManager.Initialize() 완료 후 호출 필수
    /// </summary>
    public async UniTask InitializeLocalizeCSVAsync(CancellationToken cancellationToken)
    {
        if (isInitialized)
            return;

        // 1. 언어 설정 로드 (PlayerPrefs 또는 시스템 언어)
        languagePreferences.LoadLanguage();

        // 2. CSV 데이터를 Dictionary로 캐싱
        dataProvider.BuildLocalizationDictionary();

        // 3. LocalizationSettings 로드 (비동기)
        await fontProvider.LoadSettingsAsync(cancellationToken);

        isInitialized = true;

        Debug.Log($"[LocalizationManager] 초기화 완료 - 현재 언어: {CurrentLanguage}");

        // 초기화 완료 후 이벤트 발행하여 모든 LocalizedText 컴포넌트 업데이트
        OnLanguageChanged?.Invoke(CurrentLanguage);
    }

    /// <summary>
    /// 언어 변경
    /// </summary>
    public void SetLanguage(LanguageType language)
    {
        if (CurrentLanguage == language)
            return;

        // 언어 설정 변경 및 저장
        languagePreferences.SetLanguage(language);

        // 이벤트 발행 - 모든 LocalizedText 컴포넌트 갱신
        OnLanguageChanged?.Invoke(language);

        Debug.Log($"[LocalizationManager] 언어 변경됨: {language}");
    }

    /// <summary>
    /// 키로 번역된 텍스트 조회
    /// </summary>
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!isInitialized)
        {
            Debug.LogWarning("[LocalizationManager] 아직 초기화되지 않았습니다. InitializeLocalizeCSVAsync()를 먼저 호출하세요.");
            return $"[{key}]";
        }

        // DataProvider에게 위임
        return dataProvider.GetText(key, CurrentLanguage);
    }

    /// <summary>
    /// 포맷팅된 텍스트 조회
    /// 예: "점수: {0}" → "점수: 1000"
    /// </summary>
    public string GetText(string key, params object[] args)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[LocalizationManager] 아직 초기화되지 않았습니다. InitializeLocalizeCSVAsync()를 먼저 호출하세요.");
            return $"[{key}]";
        }

        // DataProvider에게 위임
        return dataProvider.GetFormattedText(key, CurrentLanguage, args);
    }

    /// <summary>
    /// 현재 언어에 맞는 폰트 반환
    /// </summary>
    public TMP_FontAsset GetCurrentFont()
    {
        if (!fontProvider.IsLoaded)
        {
            Debug.LogWarning("[LocalizationManager] LocalizationSettings가 로드되지 않았습니다.");
            return null;
        }

        // FontProvider에게 위임
        return fontProvider.GetFont(CurrentLanguage);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: 시스템 언어 감지
    /// </summary>
    public LanguageType GetEditorLanguage()
    {
        // 런타임이면 현재 설정된 언어 반환
        if (Application.isPlaying && isInitialized)
            return CurrentLanguage;

        // 에디터 모드에서는 시스템 언어 감지
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

    /// <summary>
    /// 에디터 전용: CSV 파일을 동기적으로 직접 로드하여 텍스트 조회
    /// 런타임이 아닐 때 LocalizedText 컴포넌트에서 사용
    /// </summary>
    public string GetTextInEditor(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        LanguageType editorLanguage = GetEditorLanguage();

        // DataProvider에게 위임
        return dataProvider.GetTextInEditor(key, editorLanguage);
    }

    /// <summary>
    /// 에디터 전용: 폰트를 동기적으로 로드하여 반환
    /// LocalizedText 컴포넌트의 에디터 미리보기용
    /// </summary>
    public TMP_FontAsset GetCurrentFontInEditor()
    {
        // 런타임이면 일반 GetCurrentFont() 사용
        if (Application.isPlaying && isInitialized)
        {
            return GetCurrentFont();
        }

        // 에디터 모드에서 폰트 로드
        LanguageType editorLanguage = GetEditorLanguage();

        // FontProvider에게 위임
        return fontProvider.GetFontInEditor(editorLanguage);
    }
#endif
}
