using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
#endif

/// <summary>
/// 로컬라이징 데이터 제공자
/// CSV 데이터 로딩 및 텍스트 조회 전담
/// </summary>
public class LocalizationDataProvider
{
    private Dictionary<string, LocalizationData> localizationDict;
    private readonly Dictionary<string, string> missingKeyCache;

#if UNITY_EDITOR
    // 에디터 전용 CSV 캐시
    private static Dictionary<string, LocalizationData> editorCache;
    private static DateTime lastCsvModifiedTime;
#endif

    /// <summary>
    /// 생성자
    /// </summary>
    public LocalizationDataProvider()
    {
        localizationDict = new Dictionary<string, LocalizationData>();
        missingKeyCache = new Dictionary<string, string>();
    }

    /// <summary>
    /// CSV 데이터를 Dictionary로 캐싱 (빠른 조회용)
    /// </summary>
    public void BuildLocalizationDictionary()
    {
        localizationDict.Clear();

        List<LocalizationData> dataList = CSVManager.Instance.GetTable<LocalizationData>();

        if (dataList == null || dataList.Count == 0)
        {
            Debug.LogWarning("[LocalizationDataProvider] LocalizationData가 비어있습니다. CSV 파일을 확인하세요.");
            return;
        }

        // LINQ 사용 금지 - foreach 사용
        for (int i = 0; i < dataList.Count; i++)
        {
            LocalizationData data = dataList[i];

            if (string.IsNullOrEmpty(data.Key))
            {
                Debug.LogWarning($"[LocalizationDataProvider] 빈 키 발견 (행 {i + 1}), 스킵합니다.");
                continue;
            }

            if (localizationDict.ContainsKey(data.Key))
            {
                Debug.LogWarning($"[LocalizationDataProvider] 중복 키 발견: {data.Key}");
                continue;
            }

            localizationDict[data.Key] = data;
        }

        Debug.Log($"[LocalizationDataProvider] 로컬라이징 데이터 로드 완료: {localizationDict.Count}개");
    }

    /// <summary>
    /// 키로 번역된 텍스트 조회
    /// </summary>
    public string GetText(string key, LanguageType language)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!localizationDict.TryGetValue(key, out LocalizationData data))
        {
            Debug.LogWarning($"[LocalizationDataProvider] 키를 찾을 수 없음: {key}");
            return GetMissingKeyText(key);
        }

        // switch-case로 직접 필드 접근 (리플렉션 제거 - 100배 이상 성능 향상)
        string text;
        switch (language)
        {
            case LanguageType.Korean:
                text = data.Korean;
                break;
            case LanguageType.English:
                text = data.English;
                break;
            default:
                text = string.Empty;
                break;
        }

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning($"[LocalizationDataProvider] 키 '{key}'의 '{language}' 번역이 비어있습니다.");
            return GetMissingKeyText(key);
        }

        return text;
    }

    /// <summary>
    /// 포맷팅된 텍스트 조회
    /// 예: "점수: {0}" → "점수: 1000"
    /// </summary>
    public string GetFormattedText(string key, LanguageType language, params object[] args)
    {
        string format = GetText(key, language);

        if (format.StartsWith("[") && format.EndsWith("]"))
            return format;

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException e)
        {
            Debug.LogError($"[LocalizationDataProvider] 포맷 오류: {key}\n{e.Message}");
            return format;
        }
    }

    /// <summary>
    /// 누락된 키에 대한 텍스트 반환 (캐싱)
    /// GC Allocation 방지
    /// </summary>
    private string GetMissingKeyText(string key)
    {
        // 캐시에서 조회
        if (!missingKeyCache.TryGetValue(key, out string cached))
        {
            cached = $"[{key}]";
            missingKeyCache[key] = cached;
        }
        return cached;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: CSV 파일이 변경되었는지 확인
    /// </summary>
    private bool HasCSVFileChanged()
    {
        string csvPath = Path.Combine(Application.dataPath, "Data/CSV/LocalizationData.csv");
        var fileInfo = new FileInfo(csvPath);

        if (!fileInfo.Exists)
            return false;

        if (fileInfo.LastWriteTime != lastCsvModifiedTime)
        {
            lastCsvModifiedTime = fileInfo.LastWriteTime;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 에디터 전용: CSV 파일을 로드하여 캐시에 저장
    /// </summary>
    private void LoadCSVIntoEditorCache()
    {
        editorCache = new Dictionary<string, LocalizationData>();
        string csvPath = Path.Combine(Application.dataPath, "Data/CSV/LocalizationData.csv");

        if (!File.Exists(csvPath))
            return;

        try
        {
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 2)
                return;

            // 헤더 파싱
            string[] headers = lines[0].Split(',');
            int keyIndex = -1;
            int koreanIndex = -1;
            int englishIndex = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();
                if (header == "Key") keyIndex = i;
                else if (header == "Korean") koreanIndex = i;
                else if (header == "English") englishIndex = i;
            }

            if (keyIndex < 0)
                return;

            // 데이터 행 파싱
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] values = line.Split(',');
                if (values.Length <= keyIndex)
                    continue;

                string rowKey = values[keyIndex].Trim();
                if (string.IsNullOrEmpty(rowKey))
                    continue;

                var data = new LocalizationData();
                data.Key = rowKey;

                if (koreanIndex >= 0 && values.Length > koreanIndex)
                    data.Korean = values[koreanIndex].Trim();

                if (englishIndex >= 0 && values.Length > englishIndex)
                    data.English = values[englishIndex].Trim();

                editorCache[rowKey] = data;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[LocalizationDataProvider] 에디터 CSV 캐시 로드 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 에디터 전용: CSV 파일을 동기적으로 직접 로드하여 텍스트 조회
    /// 런타임이 아닐 때 LocalizedText 컴포넌트에서 사용
    /// </summary>
    public string GetTextInEditor(string key, LanguageType language)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        // 캐시가 없거나 CSV 파일이 변경되었으면 다시 로드
        if (editorCache == null || HasCSVFileChanged())
        {
            LoadCSVIntoEditorCache();
        }

        // 캐시에서 조회
        if (editorCache != null && editorCache.TryGetValue(key, out LocalizationData data))
        {
            string text;
            switch (language)
            {
                case LanguageType.Korean:
                    text = data.Korean;
                    break;
                case LanguageType.English:
                    text = data.English;
                    break;
                default:
                    text = string.Empty;
                    break;
            }

            return string.IsNullOrEmpty(text) ? GetMissingKeyText(key) : text;
        }

        return GetMissingKeyText(key);
    }
#endif
}
