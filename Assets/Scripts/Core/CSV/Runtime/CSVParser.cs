using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// CSV 파일 파싱 유틸리티 (개선 버전)
/// Addressable 시스템을 활용하여 CSV를 자동으로 클래스 리스트로 변환
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// CSV 파일이 위치한 Root 경로
    /// </summary>
    public static string RootPath { get; set; } = "Assets/Data/CSV";

    /// <summary>
    /// 파싱 모드
    /// </summary>
    public enum ParseMode
    {
        Lenient,  // 변환 실패 시 기본값 사용
        Strict    // 변환 실패 시 행 전체 스킵
    }

    /// <summary>
    /// 컬럼 매퍼 (리플렉션 캐싱용)
    /// </summary>
    private class ColumnMapper
    {
        public string HeaderName;
        public FieldInfo Field;
        public PropertyInfo Property;
        public Type TargetType;
        public bool IsNullable;
        public Type UnderlyingType;
    }

    /// <summary>
    /// CSV 파일을 비동기로 파싱하여 List<T> 반환
    /// </summary>
    public static async UniTask<List<T>> ParseAsync<T>(
        string fileName,
        CancellationToken cancellationToken = default,
        ParseMode mode = ParseMode.Lenient) where T : new()
    {
        string fullPath = $"{RootPath}/{fileName}.csv";

        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(fullPath);

        try
        {
            TextAsset csvFile = await handle.ToUniTask(cancellationToken: cancellationToken);

            if (csvFile == null)
            {
                Debug.LogError($"[CSVParser] CSV 파일을 찾을 수 없습니다: {fullPath}");
                return new List<T>();
            }

            List<T> result = ParseCSVText<T>(csvFile.text, fullPath, mode);

            Debug.Log($"[CSVParser] {fullPath} 파싱 완료: {result.Count}개 항목");
            return result;
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[CSVParser] CSV 로드 취소됨: {fullPath}");
            return new List<T>();
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVParser] CSV 로드 실패: {fullPath}\n{e.Message}");
            return new List<T>();
        }
        finally
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    /// <summary>
    /// CSV 텍스트를 파싱하여 List<T> 반환
    /// </summary>
    private static List<T> ParseCSVText<T>(string csvText, string filePath, ParseMode mode) where T : new()
    {
        // 1. 줄 단위로 분할
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length < 2)
        {
            Debug.LogError($"[CSVParser] CSV 파일이 비어있거나 헤더만 존재합니다: {filePath}");
            return new List<T>();
        }

        // 2. 헤더 파싱
        string[] headers = SplitCSVLine(lines[0]);

        if (headers.Length == 0)
        {
            Debug.LogError($"[CSVParser] 헤더를 파싱할 수 없습니다: {filePath}");
            return new List<T>();
        }

        // 3. 컬럼 매퍼 생성 (리플렉션 캐싱)
        List<ColumnMapper> columnMappers = BuildColumnMappers<T>(headers);

        // 4. 결과 리스트 생성 (capacity 최적화)
        int estimatedRows = lines.Length - 1;
        List<T> result = new List<T>(estimatedRows);

        Type type = typeof(T);

        // 5. 데이터 행 파싱
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = SplitCSVLine(line);

            if (values.Length != headers.Length)
            {
                Debug.LogWarning($"[CSVParser] 라인 {i + 1}의 컬럼 수가 헤더와 다릅니다. 스킵합니다.");
                continue;
            }

            // 인스턴스 생성
            T instance = new T();
            bool hasError = false;

            // 각 컬럼 값 할당
            for (int j = 0; j < columnMappers.Count; j++)
            {
                ColumnMapper mapper = columnMappers[j];
                string value = values[j].Trim();

                // CSV 인젝션 방어
                if (IsCSVInjectionRisk(value))
                {
                    Debug.LogWarning($"[CSVParser] CSV 인젝션 위험 감지: {value} (라인 {i + 1})");
                    value = "'" + value; // 이스케이프 처리
                }

                // 값 변환
                object convertedValue = ConvertValue(value, mapper.TargetType, mapper.IsNullable, mapper.UnderlyingType);

                if (convertedValue == null && !mapper.IsNullable && mapper.TargetType.IsValueType)
                {
                    if (mode == ParseMode.Strict)
                    {
                        Debug.LogWarning($"[CSVParser] 변환 실패로 라인 {i + 1} 스킵 (Strict 모드)");
                        hasError = true;
                        break;
                    }
                }

                // 필드 또는 프로퍼티에 값 할당
                if (mapper.Field != null)
                {
                    mapper.Field.SetValue(instance, convertedValue);
                }
                else if (mapper.Property != null)
                {
                    mapper.Property.SetValue(instance, convertedValue);
                }
            }

            if (!hasError)
            {
                result.Add(instance);
            }
        }

        return result;
    }

    /// <summary>
    /// 컬럼 매퍼 생성 (리플렉션 결과 캐싱)
    /// </summary>
    private static List<ColumnMapper> BuildColumnMappers<T>(string[] headers)
    {
        List<ColumnMapper> mappers = new List<ColumnMapper>(headers.Length);
        Type type = typeof(T);

        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim();

            ColumnMapper mapper = new ColumnMapper
            {
                HeaderName = headerName
            };

            // 필드 찾기
            FieldInfo field = type.GetField(headerName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                mapper.Field = field;
                mapper.TargetType = field.FieldType;
            }
            else
            {
                // 프로퍼티 찾기
                PropertyInfo property = type.GetProperty(headerName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property != null && property.CanWrite)
                {
                    mapper.Property = property;
                    mapper.TargetType = property.PropertyType;
                }
                else
                {
                    Debug.LogWarning($"[CSVParser] 필드/프로퍼티를 찾을 수 없습니다: {headerName} (타입: {type.Name})");
                    mappers.Add(mapper);
                    continue;
                }
            }

            // Nullable 타입 체크
            Type underlyingType = Nullable.GetUnderlyingType(mapper.TargetType);
            if (underlyingType != null)
            {
                mapper.IsNullable = true;
                mapper.UnderlyingType = underlyingType;
            }
            else
            {
                mapper.IsNullable = false;
                mapper.UnderlyingType = mapper.TargetType;
            }

            mappers.Add(mapper);
        }

        return mappers;
    }

    /// <summary>
    /// CSV 라인을 컬럼으로 분할 (따옴표 처리 포함)
    /// </summary>
    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        int startIndex = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                string value = line.Substring(startIndex, i - startIndex);
                result.Add(CleanValue(value));
                startIndex = i + 1;
            }
        }

        // 마지막 값 추가
        if (startIndex < line.Length)
        {
            string value = line.Substring(startIndex);
            result.Add(CleanValue(value));
        }
        else if (line.EndsWith(","))
        {
            result.Add("");
        }

        return result.ToArray();
    }

    /// <summary>
    /// 값 정리 (따옴표 제거 및 이스케이프 처리)
    /// </summary>
    private static string CleanValue(string value)
    {
        value = value.Trim();

        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2);
            value = value.Replace("\"\"", "\"");
        }

        return value;
    }

    /// <summary>
    /// CSV 인젝션 위험 체크
    /// </summary>
    private static bool IsCSVInjectionRisk(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        char firstChar = value[0];
        return firstChar == '=' || firstChar == '+' || firstChar == '-' || firstChar == '@';
    }

    /// <summary>
    /// 문자열을 특정 타입으로 변환 (Nullable 지원)
    /// </summary>
    private static object ConvertValue(string value, Type targetType, bool isNullable, Type underlyingType)
    {
        try
        {
            // 빈 문자열 처리
            if (string.IsNullOrEmpty(value))
            {
                if (isNullable)
                    return null;

                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);

                return null;
            }

            Type typeToConvert = isNullable ? underlyingType : targetType;

            // 복합 타입 처리 (배열, 리스트, 딕셔너리, 커스텀 클래스)
            if (CSVComplexTypeParser.IsComplexType(typeToConvert, out ComplexTypeKind kind))
            {
                switch (kind)
                {
                    case ComplexTypeKind.Array:
                        return CSVComplexTypeParser.ParseArray(value, typeToConvert);

                    case ComplexTypeKind.List:
                        return CSVComplexTypeParser.ParseList(value, typeToConvert);

                    case ComplexTypeKind.Dictionary:
                        return CSVComplexTypeParser.ParseDictionary(value, typeToConvert);

                    case ComplexTypeKind.CustomType:
                        return CSVComplexTypeParser.ParseCustomType(value, typeToConvert);
                }
            }

            // Enum 처리 (TryParse 사용)
            if (typeToConvert.IsEnum)
            {
                if (Enum.TryParse(typeToConvert, value, true, out object enumValue))
                {
                    return enumValue;
                }

                Debug.LogError($"[CSVParser] Enum 변환 실패: '{value}' → {typeToConvert.Name}");
                return Activator.CreateInstance(typeToConvert);
            }

            // string 타입
            if (typeToConvert == typeof(string))
            {
                return value;
            }

            // bool 특수 처리
            if (typeToConvert == typeof(bool))
            {
                string lowerValue = value.ToLower();
                if (lowerValue == "true" || lowerValue == "1")
                    return true;
                if (lowerValue == "false" || lowerValue == "0")
                    return false;
            }

            // 기본 타입 변환
            return Convert.ChangeType(value, typeToConvert);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVParser] 값 변환 실패: '{value}' → {targetType.Name}\n{e.Message}");

            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);

            return null;
        }
    }
}
