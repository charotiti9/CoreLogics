using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Core.Utilities;

/// <summary>
/// CSV 테이블간 참조를 자동으로 해결
/// </summary>
public static class CSVReferenceResolver
{
    /// <summary>
    /// 모든 참조 필드를 실제 객체로 연결
    /// </summary>
    /// <typeparam name="T">대상 타입</typeparam>
    /// <param name="dataList">데이터 리스트</param>
    /// <param name="allTables">모든 테이블 딕셔너리 (테이블명 → 데이터)</param>
    public static void ResolveReferences<T>(List<T> dataList, Dictionary<string, object> allTables)
    {
        if (dataList == null || dataList.Count == 0)
            return;

        Type type = typeof(T);

        // CSVReference Attribute가 붙은 필드 찾기
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        List<FieldInfo> referenceFields = new List<FieldInfo>();

        for (int i = 0; i < fields.Length; i++)
        {
            CSVReferenceAttribute attr = fields[i].GetCustomAttribute<CSVReferenceAttribute>();
            if (attr != null)
            {
                referenceFields.Add(fields[i]);
            }
        }

        if (referenceFields.Count == 0)
            return;

        // 각 데이터 행의 참조 해결
        for (int i = 0; i < dataList.Count; i++)
        {
            T data = dataList[i];

            for (int j = 0; j < referenceFields.Count; j++)
            {
                FieldInfo refField = referenceFields[j];
                CSVReferenceAttribute attr = refField.GetCustomAttribute<CSVReferenceAttribute>();

                // 참조 테이블 가져오기
                if (!allTables.TryGetValue(attr.ReferenceTableName, out object refTable))
                {
                    GameLogger.LogError($"[CSVReferenceResolver] 참조 테이블 없음: {attr.ReferenceTableName}");
                    continue;
                }

                // ICSVData 타입 필드가 아니면 스킵
                if (!typeof(ICSVData).IsAssignableFrom(refField.FieldType))
                {
                    GameLogger.LogWarning($"[CSVReferenceResolver] ICSVData 타입이 아님: {refField.Name} in {type.Name}");
                    continue;
                }

                // 임시 인스턴스에서 키 값 추출
                object idValue = null;
                object tempInstance = refField.GetValue(data);
                if (tempInstance != null)
                {
                    FieldInfo keyField = refField.FieldType.GetField(attr.ReferenceColumnName);
                    if (keyField != null)
                    {
                        idValue = keyField.GetValue(tempInstance);
                    }
                }

                if (idValue == null)
                {
                    // ID가 null이면 참조도 null
                    refField.SetValue(data, null);
                    continue;
                }

                // 참조 객체 찾기
                object refObject = FindByID(refTable, attr.ReferenceColumnName, idValue);

                if (refObject != null)
                {
                    refField.SetValue(data, refObject);
                }
                else
                {
                    GameLogger.LogWarning($"[CSVReferenceResolver] 참조 객체 찾기 실패: {attr.ReferenceTableName}.{attr.ReferenceColumnName} = {idValue}");
                }
            }
        }
    }

    /// <summary>
    /// 테이블에서 ID로 객체 찾기
    /// </summary>
    private static object FindByID(object table, string columnName, object idValue)
    {
        Type listType = table.GetType();

        if (!listType.IsGenericType)
            return null;

        Type elementType = listType.GetGenericArguments()[0];
        System.Collections.IEnumerable enumerable = (System.Collections.IEnumerable)table;

        FieldInfo idFieldInfo = elementType.GetField(columnName);
        if (idFieldInfo == null)
        {
            GameLogger.LogError($"[CSVReferenceResolver] 컬럼 없음: {columnName} in {elementType.Name}");
            return null;
        }

        foreach (object item in enumerable)
        {
            object value = idFieldInfo.GetValue(item);
            if (value != null && value.Equals(idValue))
            {
                return item;
            }
        }

        return null;
    }
}
