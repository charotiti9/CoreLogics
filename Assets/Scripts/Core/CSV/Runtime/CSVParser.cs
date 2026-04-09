using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Core.Utilities;
using Core.Addressable;

/// <summary>
/// CSV 파일 파싱 유틸리티 (개선 버전)
/// Addressable 시스템을 활용하여 CSV를 자동으로 클래스 리스트로 변환
/// </summary>
public static class CSVParser
{
    private sealed class CSVRecord
    {
        public int StartLineNumber;
        public string[] Fields;
    }

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
        public bool ShouldSkip; // IsRequired=FALSE인 컬럼은 스킵
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

        try
        {
            // 스키마 파일이 아닌 경우, 스키마를 먼저 로드하여 필수 컬럼 목록 확인
            HashSet<string> requiredColumns = null;
            if (!fileName.EndsWith("_Schema"))
            {
                requiredColumns = await LoadRequiredColumnsAsync(fileName, cancellationToken);
            }

            // AddressableLoader를 통한 로드
            TextAsset csvFile = await AddressableLoader.Instance
                .LoadAssetAsync<TextAsset>(fullPath, cancellationToken);

            if (csvFile == null)
            {
                GameLogger.LogError($"[CSVParser] CSV 파일을 찾을 수 없습니다: {fullPath}");
                return new List<T>();
            }

            List<T> result = ParseCSVText<T>(csvFile.text, fullPath, mode, requiredColumns);
            return result;
        }
        catch (OperationCanceledException)
        {
            GameLogger.LogWarning($"[CSVParser] CSV 로드 취소됨: {fullPath}");
            return new List<T>();
        }
        catch (Exception e)
        {
            GameLogger.LogError($"[CSVParser] CSV 로드 실패: {fullPath}\n{e.Message}");
            return new List<T>();
        }
        finally
        {
            // 사용 완료 후 Release (참조 카운트 감소)
            AddressableLoader.Instance.Release(fullPath);
        }
    }

    /// <summary>
    /// 스키마 파일에서 IsRequired=TRUE인 컬럼 목록 로드
    /// </summary>
    private static async UniTask<HashSet<string>> LoadRequiredColumnsAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        string schemaPath = $"{RootPath}/{tableName}_Schema.csv";
        TextAsset schemaFile = null;

        try
        {
            schemaFile = await AddressableLoader.Instance
                .LoadAssetAsync<TextAsset>(schemaPath, cancellationToken);

            if (schemaFile == null)
            {
                return null; // 스키마 없으면 모든 컬럼 허용
            }

            HashSet<string> requiredColumns = new HashSet<string>();
            List<CSVRecord> records = ParseRecords(schemaFile.text, schemaPath);

            // 헤더 스킵, 데이터만 읽기
            for (int i = 1; i < records.Count; i++)
            {
                CSVRecord record = records[i];
                if (record.Fields == null || record.Fields.Length < 3)
                {
                    continue;
                }

                string columnName = record.Fields[0].Trim();
                string isRequiredStr = record.Fields[2].Trim().ToUpper();
                bool isRequired = isRequiredStr == "TRUE" || isRequiredStr == "1";

                if (isRequired)
                {
                    requiredColumns.Add(columnName);
                }
            }

            return requiredColumns;
        }
        catch (Exception e)
        {
            GameLogger.LogError($"[CSVParser] 스키마 로드 실패: {schemaPath}\n{e.Message}");
            throw;
        }
        finally
        {
            // 로드 성공한 경우에만 Release
            if (schemaFile != null)
            {
                AddressableLoader.Instance.Release(schemaPath);
            }
        }
    }

    /// <summary>
    /// CSV 텍스트를 파싱하여 List<T> 반환
    /// </summary>
    private static List<T> ParseCSVText<T>(string csvText, string filePath, ParseMode mode, HashSet<string> requiredColumns = null) where T : new()
    {
        // 1. 따옴표 내부 줄바꿈을 보존한 상태로 논리 레코드 분리
        List<CSVRecord> records = ParseRecords(csvText, filePath);

        if (records.Count < 2)
        {
            GameLogger.LogError($"[CSVParser] CSV 파일이 비어있거나 헤더만 존재합니다: {filePath}");
            return new List<T>();
        }

        // 2. 헤더 파싱
        string[] headers = records[0].Fields;

        if (headers.Length == 0)
        {
            GameLogger.LogError($"[CSVParser] 헤더를 파싱할 수 없습니다: {filePath}");
            return new List<T>();
        }

        // 3. 컬럼 매퍼 생성 (리플렉션 캐싱)
        List<ColumnMapper> columnMappers = BuildColumnMappers<T>(headers, requiredColumns);

        // 4. 결과 리스트 생성 (capacity 최적화)
        int estimatedRows = records.Count - 1;
        List<T> result = new List<T>(estimatedRows);

        // 5. 데이터 행 파싱
        for (int i = 1; i < records.Count; i++)
        {
            CSVRecord record = records[i];
            if (record.Fields == null || record.Fields.Length == 0)
                continue;

            string[] values = record.Fields;

            if (values.Length != headers.Length)
            {
                GameLogger.LogWarning($"[CSVParser] 라인 {record.StartLineNumber}의 컬럼 수가 헤더와 다릅니다. 스킵합니다.");
                continue;
            }

            // 인스턴스 생성
            T instance = new T();
            bool hasError = false;

            // 각 컬럼 값 할당
            for (int j = 0; j < columnMappers.Count; j++)
            {
                ColumnMapper mapper = columnMappers[j];

                // IsRequired=FALSE인 컬럼 스킵
                if (mapper.ShouldSkip)
                    continue;

                string value = values[j].Trim();

                // CSV 인젝션 방어
                if (IsCSVInjectionRisk(value))
                {
                    GameLogger.LogWarning($"[CSVParser] CSV 인젝션 위험 감지: {value} (라인 {record.StartLineNumber})");
                    value = "'" + value; // 이스케이프 처리
                }

                // 값 변환
                object convertedValue = ConvertValue(
                    value,
                    mapper.TargetType,
                    mapper.IsNullable,
                    mapper.UnderlyingType);

                if (convertedValue == null && !mapper.IsNullable && mapper.TargetType.IsValueType)
                {
                    if (mode == ParseMode.Strict)
                    {
                        GameLogger.LogWarning($"[CSVParser] 변환 실패로 라인 {record.StartLineNumber} 스킵 (Strict 모드)");
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
    /// 따옴표 내부 줄바꿈을 포함하는 CSV를 논리 레코드 단위로 분리
    /// </summary>
    internal static List<string> SplitCSVRecords(string csvText, string filePath = null)
    {
        List<CSVRecord> parsedRecords = ParseRecords(csvText, filePath);
        List<string> records = new List<string>(parsedRecords.Count);
        for (int i = 0; i < parsedRecords.Count; i++)
        {
            string[] fields = parsedRecords[i].Fields;
            if (fields == null || fields.Length == 0)
            {
                records.Add(string.Empty);
                continue;
            }

            records.Add(string.Join(",", fields));
        }

        return records;
    }

    /// <summary>
    /// 문자 단위 상태 머신으로 CSV를 논리 레코드 단위로 파싱합니다.
    /// 큰따옴표로 감싼 멀티라인 셀과 쉼표를 모두 지원합니다.
    /// </summary>
    private static List<CSVRecord> ParseRecords(string csvText, string filePath = null)
    {
        List<CSVRecord> records = new List<CSVRecord>();

        if (string.IsNullOrEmpty(csvText))
        {
            return records;
        }

        List<string> currentRecord = new List<string>();
        System.Text.StringBuilder currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        int currentLineNumber = 1;
        int recordStartLineNumber = 1;

        for (int i = 0; i < csvText.Length; i++)
        {
            char currentChar = csvText[i];

            if (i == 0 && currentChar == '\uFEFF')
            {
                continue;
            }

            if (currentChar == '"')
            {
                if (inQuotes && i + 1 < csvText.Length && csvText[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (!inQuotes && currentChar == ',')
            {
                currentRecord.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            if (currentChar == '\r' || currentChar == '\n')
            {
                bool isCrLf = currentChar == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n';

                if (inQuotes)
                {
                    currentField.Append('\n');
                }
                else
                {
                    currentRecord.Add(currentField.ToString());
                    currentField.Clear();

                    records.Add(new CSVRecord
                    {
                        StartLineNumber = recordStartLineNumber,
                        Fields = currentRecord.ToArray()
                    });

                    currentRecord.Clear();
                    recordStartLineNumber = currentLineNumber + 1;
                }

                if (isCrLf)
                {
                    i++;
                }

                currentLineNumber++;
                continue;
            }

            currentField.Append(currentChar);
        }

        if (inQuotes)
        {
            string targetPath = string.IsNullOrEmpty(filePath) ? "알 수 없는 CSV" : filePath;
            GameLogger.LogWarning($"[CSVParser] 닫히지 않은 따옴표가 있는 레코드를 감지했습니다: {targetPath}");
        }

        if (currentField.Length > 0 || currentRecord.Count > 0)
        {
            currentRecord.Add(currentField.ToString());
            records.Add(new CSVRecord
            {
                StartLineNumber = recordStartLineNumber,
                Fields = currentRecord.ToArray()
            });
        }

        TrimTrailingEmptyRecord(records);
        return records;
    }

    /// <summary>
    /// 컬럼 매퍼 생성 (리플렉션 결과 캐싱)
    /// </summary>
    private static List<ColumnMapper> BuildColumnMappers<T>(string[] headers, HashSet<string> requiredColumns = null)
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

            // 스키마에서 IsRequired=FALSE인 컬럼은 무시 (경고 없이)
            if (requiredColumns != null && !requiredColumns.Contains(headerName))
            {
                mapper.ShouldSkip = true;
                mappers.Add(mapper);
                continue;
            }

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
                    GameLogger.LogWarning($"[CSVParser] 필드/프로퍼티를 찾을 수 없습니다: {headerName} (타입: {type.Name})");
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

    private static void TrimTrailingEmptyRecord(List<CSVRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        CSVRecord lastRecord = records[records.Count - 1];
        if (lastRecord.Fields == null || lastRecord.Fields.Length != 1)
        {
            return;
        }

        if (!string.IsNullOrEmpty(lastRecord.Fields[0]))
        {
            return;
        }

        records.RemoveAt(records.Count - 1);
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

            // ICSVData 타입 처리 (다른 CSV 테이블 참조)
            // 임시 인스턴스를 생성하고 키 값을 저장, 나중에 CSVReferenceResolver에서 실제 객체로 교체
            if (typeof(ICSVData).IsAssignableFrom(typeToConvert))
            {
                return CreateTempCSVDataInstance(value, typeToConvert);
            }

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

                GameLogger.LogError($"[CSVParser] Enum 변환 실패: '{value}' → {typeToConvert.Name}");
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
            GameLogger.LogError($"[CSVParser] 값 변환 실패: '{value}' → {targetType.Name}\n{e.Message}");

            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);

            return null;
        }
    }

    /// <summary>
    /// ICSVData 타입의 임시 인스턴스 생성
    /// 문자열 키를 첫 번째 string 필드에 저장하여 나중에 참조 해결에 사용
    /// </summary>
    private static object CreateTempCSVDataInstance(string keyValue, Type targetType)
    {
        try
        {
            // 임시 인스턴스 생성
            object instance = Activator.CreateInstance(targetType);

            // 첫 번째 string 필드를 찾아 키 값 저장 (보통 Key 또는 ID)
            FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(string))
                {
                    fields[i].SetValue(instance, keyValue);
                    break;
                }
            }

            return instance;
        }
        catch (Exception e)
        {
            GameLogger.LogError($"[CSVParser] ICSVData 임시 인스턴스 생성 실패: {targetType.Name}\n{e.Message}");
            return null;
        }
    }
}
