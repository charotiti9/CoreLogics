using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Utilities;

/// <summary>
/// 복합 타입 종류
/// </summary>
public enum ComplexTypeKind
{
    Array,        // int[], string[] 등
    List,         // List<int>, List<string> 등
    Dictionary,   // Dictionary<int, string> 등
    CustomType    // 커스텀 클래스/구조체 (JSON 직렬화)
}

/// <summary>
/// 복합 타입(배열, 리스트, 딕셔너리, 커스텀 클래스) 파싱
/// CSV 형식: 배열/리스트 = "1;2;3", 딕셔너리 = "key1:val1;key2:val2", 커스텀 = JSON
/// </summary>
public static class CSVComplexTypeParser
{
    private const char ARRAY_SEPARATOR = ';';
    private const char DICTIONARY_KV_SEPARATOR = ':';

    /// <summary>
    /// 타입이 복합 타입인지 확인
    /// </summary>
    /// <param name="type">확인할 타입</param>
    /// <param name="kind">복합 타입 종류 (출력)</param>
    /// <returns>복합 타입이면 true</returns>
    public static bool IsComplexType(Type type, out ComplexTypeKind kind)
    {
        // 배열 체크
        if (type.IsArray)
        {
            kind = ComplexTypeKind.Array;
            return true;
        }

        // 제네릭 타입 체크
        if (type.IsGenericType)
        {
            Type genericDef = type.GetGenericTypeDefinition();

            // List<T>
            if (genericDef == typeof(List<>))
            {
                kind = ComplexTypeKind.List;
                return true;
            }

            // Dictionary<K, V>
            if (genericDef == typeof(Dictionary<,>))
            {
                kind = ComplexTypeKind.Dictionary;
                return true;
            }
        }

        // 커스텀 타입 (클래스 또는 구조체, string 제외)
        if ((type.IsClass || type.IsValueType) &&
            type != typeof(string) &&
            !type.IsPrimitive &&
            !type.IsEnum)
        {
            // [Serializable] 속성이 있는지 체크
            if (Attribute.IsDefined(type, typeof(SerializableAttribute)))
            {
                kind = ComplexTypeKind.CustomType;
                return true;
            }
        }

        kind = default;
        return false;
    }

    /// <summary>
    /// 배열 파싱: "1;2;3" → int[]
    /// </summary>
    public static object ParseArray(string value, Type arrayType)
    {
        Type elementType = arrayType.GetElementType();

        if (string.IsNullOrEmpty(value))
        {
            // 빈 배열 반환
            return Array.CreateInstance(elementType, 0);
        }

        // 세미콜론으로 분할
        string[] parts = value.Split(new[] { ARRAY_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

        // 배열 생성
        Array array = Array.CreateInstance(elementType, parts.Length);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();

            try
            {
                // 단일 값 변환
                object converted = ConvertSingleValue(part, elementType);
                array.SetValue(converted, i);
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVComplexTypeParser] 배열 요소 변환 실패: '{part}' → {elementType.Name}\n{e.Message}");

                // 기본값 설정
                if (elementType.IsValueType)
                {
                    array.SetValue(Activator.CreateInstance(elementType), i);
                }
                else
                {
                    array.SetValue(null, i);
                }
            }
        }

        return array;
    }

    /// <summary>
    /// 리스트 파싱: "a;b;c" → List<string>
    /// </summary>
    public static object ParseList(string value, Type listType)
    {
        Type elementType = listType.GetGenericArguments()[0];

        // List<T> 인스턴스 생성
        var list = Activator.CreateInstance(listType);
        var addMethod = listType.GetMethod("Add");

        if (string.IsNullOrEmpty(value))
        {
            return list;
        }

        // 세미콜론으로 분할
        string[] parts = value.Split(new[] { ARRAY_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();

            try
            {
                // 단일 값 변환
                object converted = ConvertSingleValue(part, elementType);
                addMethod.Invoke(list, new[] { converted });
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVComplexTypeParser] 리스트 요소 변환 실패: '{part}' → {elementType.Name}\n{e.Message}");
            }
        }

        return list;
    }

    /// <summary>
    /// 딕셔너리 파싱: "key1:val1;key2:val2" → Dictionary<K, V>
    /// </summary>
    public static object ParseDictionary(string value, Type dictionaryType)
    {
        Type[] genericArgs = dictionaryType.GetGenericArguments();
        Type keyType = genericArgs[0];
        Type valueType = genericArgs[1];

        // Dictionary<K, V> 인스턴스 생성
        var dictionary = Activator.CreateInstance(dictionaryType);
        var addMethod = dictionaryType.GetMethod("Add");

        if (string.IsNullOrEmpty(value))
        {
            return dictionary;
        }

        // 세미콜론으로 페어 분할
        string[] pairs = value.Split(new[] { ARRAY_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < pairs.Length; i++)
        {
            string pair = pairs[i].Trim();

            // 콜론으로 key-value 분할
            string[] kv = pair.Split(new[] { DICTIONARY_KV_SEPARATOR }, 2);

            if (kv.Length != 2)
            {
                GameLogger.LogWarning($"[CSVComplexTypeParser] 잘못된 딕셔너리 형식: '{pair}' (올바른 형식: key:value)");
                continue;
            }

            try
            {
                string keyStr = kv[0].Trim();
                string valueStr = kv[1].Trim();

                // key와 value 변환
                object key = ConvertSingleValue(keyStr, keyType);
                object val = ConvertSingleValue(valueStr, valueType);

                addMethod.Invoke(dictionary, new[] { key, val });
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVComplexTypeParser] 딕셔너리 페어 변환 실패: '{pair}'\n{e.Message}");
            }
        }

        return dictionary;
    }

    /// <summary>
    /// 커스텀 클래스/구조체 파싱 (JSON 형식)
    /// </summary>
    public static object ParseCustomType(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
        {
            // 기본값 반환
            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }

        try
        {
            // Unity JsonUtility 사용
            object instance = JsonUtility.FromJson(value, targetType);

            if (instance == null)
            {
                GameLogger.LogWarning($"[CSVComplexTypeParser] JSON 파싱 결과가 null: {targetType.Name}");

                if (targetType.IsValueType)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            return instance;
        }
        catch (Exception e)
        {
            GameLogger.LogError($"[CSVComplexTypeParser] JSON 파싱 실패: {targetType.Name}\nJSON: {value}\n{e.Message}");

            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);
            return null;
        }
    }

    /// <summary>
    /// 단일 값을 지정된 타입으로 변환
    /// CSVParser.ConvertValue와 동일한 로직 (기본 타입 및 Enum)
    /// </summary>
    private static object ConvertSingleValue(string value, Type targetType)
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

            // Nullable 타입 체크
            Type underlyingType = Nullable.GetUnderlyingType(targetType);
            Type typeToConvert = underlyingType ?? targetType;

            // Enum 처리
            if (typeToConvert.IsEnum)
            {
                if (Enum.TryParse(typeToConvert, value, true, out object enumValue))
                {
                    return enumValue;
                }

                GameLogger.LogError($"[CSVComplexTypeParser] Enum 변환 실패: '{value}' → {typeToConvert.Name}");
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

            // 기본 타입 변환 (int, float, double 등)
            return Convert.ChangeType(value, typeToConvert);
        }
        catch (Exception e)
        {
            GameLogger.LogError($"[CSVComplexTypeParser] 값 변환 실패: '{value}' → {targetType.Name}\n{e.Message}");

            if (targetType.IsValueType)
                return Activator.CreateInstance(targetType);

            return null;
        }
    }
}
