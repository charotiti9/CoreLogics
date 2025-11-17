using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// CSV 파일 파싱 유틸리티
/// Addressable 시스템을 활용하여 CSV를 자동으로 클래스 리스트로 변환
/// </summary>
public static class CSVParser
{
    /// <summary>
    /// CSV 파일이 위치한 Root 경로
    /// 기본값: "Assets/Data/CSV"
    /// </summary>
    public static string RootPath { get; set; } = "Assets/Data/CSV";

    /// <summary>
    /// CSV 파일을 비동기로 파싱하여 List<T> 반환
    /// </summary>
    /// <typeparam name="T">변환할 데이터 클래스 타입</typeparam>
    /// <param name="fileName">CSV 파일명 (확장자 제외)</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>파싱된 데이터 리스트</returns>
    public static async UniTask<List<T>> ParseAsync<T>(string fileName, CancellationToken cancellationToken = default) where T : new()
    {
        // 1. 전체 경로 생성
        string fullPath = $"{RootPath}/{fileName}.csv";

        // 2. Addressable로 CSV 파일 로드
        AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(fullPath);

        try
        {
            // 로드 완료 대기 (취소 가능)
            TextAsset csvFile = await handle.ToUniTask(cancellationToken: cancellationToken);

            if (csvFile == null)
            {
                Debug.LogError($"[CSVParser] CSV 파일을 찾을 수 없습니다: {fullPath}");
                return new List<T>();
            }

            // 3. CSV 텍스트 파싱
            List<T> result = ParseCSVText<T>(csvFile.text, fullPath);

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
            // 리소스 해제
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
    }

    /// <summary>
    /// CSV 텍스트를 파싱하여 List<T> 반환 (내부 메서드)
    /// </summary>
    /// <typeparam name="T">변환할 데이터 클래스 타입</typeparam>
    /// <param name="csvText">CSV 텍스트</param>
    /// <param name="filePath">파일 경로 (로그용)</param>
    /// <returns>파싱된 데이터 리스트</returns>
    private static List<T> ParseCSVText<T>(string csvText, string filePath) where T : new()
    {
        List<T> result = new List<T>();

        // 1. 줄 단위로 분할 (CR, LF, CRLF 모두 처리)
        string[] lines = csvText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length < 2)
        {
            Debug.LogError($"[CSVParser] CSV 파일이 비어있거나 헤더만 존재합니다: {filePath}");
            return result;
        }

        // 2. 헤더 파싱
        string[] headers = SplitCSVLine(lines[0]);
        if (headers.Length == 0)
        {
            Debug.LogError($"[CSVParser] 헤더를 파싱할 수 없습니다: {filePath}");
            return result;
        }

        // 3. 타입 정보 가져오기 (리플렉션)
        Type type = typeof(T);

        // 4. 데이터 줄 파싱 (헤더 제외)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // 빈 줄 스킵
            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = SplitCSVLine(line);

            // 컬럼 수가 맞지 않으면 경고
            if (values.Length != headers.Length)
            {
                Debug.LogWarning($"[CSVParser] 라인 {i + 1}의 컬럼 수가 헤더와 다릅니다. 스킵합니다.");
                continue;
            }

            // 5. T 인스턴스 생성
            T instance = new T();

            // 6. 각 컬럼을 필드/프로퍼티에 매핑
            for (int j = 0; j < headers.Length; j++)
            {
                string headerName = headers[j].Trim();
                string value = values[j].Trim();

                // 필드 찾기
                FieldInfo field = type.GetField(headerName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (field != null)
                {
                    // 타입 변환 후 할당
                    object convertedValue = ConvertValue(value, field.FieldType);
                    field.SetValue(instance, convertedValue);
                }
                else
                {
                    // 프로퍼티 찾기
                    PropertyInfo property = type.GetProperty(headerName,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                    if (property != null && property.CanWrite)
                    {
                        object convertedValue = ConvertValue(value, property.PropertyType);
                        property.SetValue(instance, convertedValue);
                    }
                    else
                    {
                        Debug.LogWarning($"[CSVParser] 필드/프로퍼티를 찾을 수 없습니다: {headerName} (타입: {type.Name})");
                    }
                }
            }

            result.Add(instance);
        }

        return result;
    }

    /// <summary>
    /// CSV 라인을 컬럼으로 분할 (따옴표 처리 포함)
    /// </summary>
    /// <param name="line">CSV 라인</param>
    /// <returns>분할된 값 배열</returns>
    private static string[] SplitCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        int startIndex = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            // 따옴표 처리
            if (c == '"')
            {
                // 다음 문자도 따옴표인지 확인 (이스케이프된 따옴표 "")
                if (i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++; // 이중 따옴표는 스킵
                }
                else
                {
                    // 따옴표 상태 토글
                    inQuotes = !inQuotes;
                }
            }
            // 쉼표 처리 (따옴표 밖에서만)
            else if (c == ',' && !inQuotes)
            {
                // 현재 위치까지의 값 추출
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
            // 마지막이 쉼표로 끝나면 빈 값 추가
            result.Add("");
        }

        return result.ToArray();
    }

    /// <summary>
    /// 값 정리 (따옴표 제거 및 이스케이프 처리)
    /// </summary>
    /// <param name="value">원본 값</param>
    /// <returns>정리된 값</returns>
    private static string CleanValue(string value)
    {
        value = value.Trim();

        // 양쪽 따옴표 제거
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2);

            // 이중 따옴표("") → 단일 따옴표(") 변환
            value = value.Replace("\"\"", "\"");
        }

        return value;
    }

    /// <summary>
    /// 문자열을 특정 타입으로 변환
    /// </summary>
    /// <param name="value">변환할 문자열</param>
    /// <param name="targetType">목표 타입</param>
    /// <returns>변환된 값</returns>
    private static object ConvertValue(string value, Type targetType)
    {
        try
        {
            // 빈 문자열 처리
            if (string.IsNullOrEmpty(value))
            {
                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            // Enum 처리
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true); // ignoreCase = true
            }

            // string 타입은 그대로 반환
            if (targetType == typeof(string))
            {
                return value;
            }

            // bool 특수 처리 ("true", "false", "1", "0")
            if (targetType == typeof(bool))
            {
                string lowerValue = value.ToLower();
                if (lowerValue == "true" || lowerValue == "1")
                    return true;
                if (lowerValue == "false" || lowerValue == "0")
                    return false;
            }

            // 기본 타입 변환
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CSVParser] 값 변환 실패: '{value}' → {targetType.Name}\n{e.Message}");

            // 기본값 반환
            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }
    }
}
