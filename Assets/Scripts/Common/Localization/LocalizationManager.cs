using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using System.IO;
#endif

/// <summary>
/// 로컬라이징 시스템 관리자
/// CSV 기반 다국어 지원 제공
/// </summary>
public class LocalizationManager : EagerSingleton<LocalizationManager>
{
    private const string LANGUAGE_PREFS_KEY = "Localization_Language";

    private LanguageType currentLanguage;
    private Dictionary<string, LocalizationData> localizationDict;
    private bool isInitialized;

    [Header("Language Fonts")]
    [SerializeField] private TMP_FontAsset koreanFont;
    [SerializeField] private TMP_FontAsset englishFont;

    /// <summary>
    /// 현재 설정된 언어
    /// </summary>
    public LanguageType CurrentLanguage => currentLanguage;

    /// <summary>
    /// 언어 변경 이벤트
    /// LocalizedText 컴포넌트들이 구독하여 자동 갱신
    /// </summary>
    public event Action<LanguageType> OnLanguageChanged;

    /// <summary>
    /// 초기화
    /// CSVManager.Initialize() 완료 후 호출 필수
    /// </summary>
    public void InitializeLocalizeCSV()
    {
        if (isInitialized)
            return;

        // PlayerPrefs에서 저장된 언어 불러오기
        LoadLanguageFromPrefs();

        // CSV 데이터를 Dictionary로 캐싱
        BuildLocalizationDictionary();

        isInitialized = true;

        Debug.Log($"[LocalizationManager] 초기화 완료 - 현재 언어: {currentLanguage}");
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 언어 설정 불러오기
    /// 저장된 값이 없으면 시스템 언어 감지
    /// </summary>
    private void LoadLanguageFromPrefs()
    {
        if (PlayerPrefs.HasKey(LANGUAGE_PREFS_KEY))
        {
            int savedLanguage = PlayerPrefs.GetInt(LANGUAGE_PREFS_KEY);
            currentLanguage = (LanguageType)savedLanguage;
        }
        else
        {
            // 저장된 언어가 없으면 시스템 언어 감지
            currentLanguage = DetectSystemLanguage();
        }
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

    /// <summary>
    /// CSV 데이터를 Dictionary로 캐싱 (빠른 조회용)
    /// </summary>
    private void BuildLocalizationDictionary()
    {
        localizationDict = new Dictionary<string, LocalizationData>();

        List<LocalizationData> dataList = CSVManager.Instance.GetTable<LocalizationData>();

        if (dataList == null || dataList.Count == 0)
        {
            Debug.LogWarning("[LocalizationManager] LocalizationData가 비어있습니다. CSV 파일을 확인하세요.");
            return;
        }

        // LINQ 사용 금지 - foreach 사용
        for (int i = 0; i < dataList.Count; i++)
        {
            LocalizationData data = dataList[i];

            if (string.IsNullOrEmpty(data.Key))
            {
                Debug.LogWarning($"[LocalizationManager] 빈 키 발견 (행 {i + 1}), 스킵합니다.");
                continue;
            }

            if (localizationDict.ContainsKey(data.Key))
            {
                Debug.LogWarning($"[LocalizationManager] 중복 키 발견: {data.Key}");
                continue;
            }

            localizationDict[data.Key] = data;
        }

        Debug.Log($"[LocalizationManager] 로컬라이징 데이터 로드 완료: {localizationDict.Count}개");
    }

    /// <summary>
    /// 언어 변경
    /// </summary>
    public void SetLanguage(LanguageType language)
    {
        if (currentLanguage == language)
            return;

        currentLanguage = language;

        // PlayerPrefs에 저장
        PlayerPrefs.SetInt(LANGUAGE_PREFS_KEY, (int)language);
        PlayerPrefs.Save();

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
            Debug.LogWarning("[LocalizationManager] 아직 초기화되지 않았습니다. Initialize()를 먼저 호출하세요.");
            return $"[{key}]";
        }

        if (!localizationDict.TryGetValue(key, out LocalizationData data))
        {
            Debug.LogWarning($"[LocalizationManager] 키를 찾을 수 없음: {key}");
            return $"[{key}]";
        }

        // 리플렉션으로 현재 언어에 맞는 필드 값 가져오기
        string languageFieldName = currentLanguage.ToString();
        FieldInfo field = typeof(LocalizationData).GetField(languageFieldName, BindingFlags.Public | BindingFlags.Instance);

        if (field == null)
        {
            Debug.LogError($"[LocalizationManager] LocalizationData에 '{languageFieldName}' 필드가 없습니다.");
            return $"[{key}]";
        }

        string text = (string)field.GetValue(data);

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[LocalizationManager] 키 '{key}'의 '{languageFieldName}' 번역이 비어있습니다.");
            return $"[{key}]";
        }

        return text;
    }

    /// <summary>
    /// 포맷팅된 텍스트 조회
    /// 예: "점수: {0}" → "점수: 1000"
    /// </summary>
    public string GetText(string key, params object[] args)
    {
        string format = GetText(key);

        if (format.StartsWith("[") && format.EndsWith("]"))
            return format;

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException e)
        {
            Debug.LogError($"[LocalizationManager] 포맷 오류: {key}\n{e.Message}");
            return format;
        }
    }

    /// <summary>
    /// 현재 언어에 맞는 폰트 반환
    /// </summary>
    public TMP_FontAsset GetCurrentFont()
    {
        switch (currentLanguage)
        {
            case LanguageType.Korean:
                return koreanFont;

            case LanguageType.English:
                return englishFont;

            default:
                return null;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: CSV 파일을 동기적으로 직접 로드하여 텍스트 조회
    /// 런타임이 아닐 때 LocalizedText 컴포넌트에서 사용
    /// </summary>
    public string GetTextInEditor(string key)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // 에디터에서 현재 언어 가져오기
        LanguageType editorLanguage = GetEditorLanguage();

        // CSV 파일 경로
        string csvPath = Path.Combine(Application.dataPath, "Data/CSV/LocalizationData.csv");

        if (!File.Exists(csvPath))
        {
            Debug.LogWarning($"[LocalizationManager] CSV 파일 없음: {csvPath}");
            return $"[{key}]";
        }

        try
        {
            // CSV 파일 동기 로드
            string[] lines = File.ReadAllLines(csvPath);

            if (lines.Length < 2)
                return $"[{key}]";

            // 헤더 파싱
            string[] headers = lines[0].Split(',');
            int keyIndex = -1;
            int languageIndex = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();

                if (header == "Key")
                    keyIndex = i;
                else if (header == editorLanguage.ToString())
                    languageIndex = i;
            }

            if (keyIndex < 0 || languageIndex < 0)
            {
                Debug.LogWarning($"[LocalizationManager] CSV 헤더 오류: Key 또는 {editorLanguage} 컬럼을 찾을 수 없습니다.");
                return $"[{key}]";
            }

            // 데이터 행 검색
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line))
                    continue;

                string[] values = line.Split(',');

                if (values.Length <= keyIndex || values.Length <= languageIndex)
                    continue;

                string rowKey = values[keyIndex].Trim();

                if (rowKey == key)
                {
                    string text = values[languageIndex].Trim();
                    return string.IsNullOrEmpty(text) ? $"[{key}]" : text;
                }
            }

            // 키를 찾지 못함
            return $"[{key}]";
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalizationManager] CSV 로드 실패: {e.Message}");
            return $"[{key}]";
        }
    }

    /// <summary>
    /// 에디터 전용: 시스템 언어 감지
    /// </summary>
    public LanguageType GetEditorLanguage()
    {
        // 런타임이면 현재 설정된 언어 반환
        if (Application.isPlaying && isInitialized)
            return currentLanguage;

        // 에디터 모드에서는 시스템 언어 감지
        return DetectSystemLanguage();
    }
#endif
}
