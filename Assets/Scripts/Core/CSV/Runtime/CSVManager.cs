using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Core.Utilities;

/// <summary>
/// 모든 CSV 데이터를 Generic하게 관리하는 싱글톤입니다.
/// </summary>
public class CSVManager : LazySingleton<CSVManager>
{
    private Dictionary<Type, object> tables = new Dictionary<Type, object>();
    private Dictionary<string, CSVSchema> schemas = new Dictionary<string, CSVSchema>();
    private List<Type> csvDataTypes = new List<Type>();

    /// <summary>
    /// 모든 CSV를 로드하고 참조를 해결합니다.
    /// </summary>
    /// <param name="cancellationToken">취소 토큰입니다.</param>
    public async UniTask Initialize(CancellationToken cancellationToken = default)
    {
        int csvTypeCount = FindAllCSVDataTypes();
        int loadedSchemaCount = await LoadAllSchemasAsync(cancellationToken);
        int loadedTableCount = await LoadAllTablesAsync(cancellationToken);
        int resolvedReferenceCount = ResolveAllReferences();

        GameLogger.Log($"[CSVManager] 초기화 완료 - 타입 {csvTypeCount}개, 스키마 {loadedSchemaCount}개, 테이블 {loadedTableCount}개, 참조 해결 {resolvedReferenceCount}개");
    }

    /// <summary>
    /// Assembly에서 ICSVData를 구현한 모든 타입을 찾습니다.
    /// </summary>
    private int FindAllCSVDataTypes()
    {
        csvDataTypes.Clear();

        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();

        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];

            if (typeof(ICSVData).IsAssignableFrom(type) &&
                !type.IsInterface &&
                !type.IsAbstract)
            {
                csvDataTypes.Add(type);
            }
        }

        return csvDataTypes.Count;
    }

    /// <summary>
    /// 모든 스키마를 로드하고 성공 개수를 반환합니다.
    /// </summary>
    private async UniTask<int> LoadAllSchemasAsync(CancellationToken cancellationToken)
    {
        int loadedSchemaCount = 0;

        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            try
            {
                CSVSchema schema = await CSVSchemaParser.ParseSchemaAsync(tableName, cancellationToken);
                schemas[tableName] = schema;
                loadedSchemaCount++;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 스키마 로드 실패: {tableName}\n{e.Message}");
            }
        }

        return loadedSchemaCount;
    }

    /// <summary>
    /// 모든 테이블을 로드하고 성공 개수를 반환합니다.
    /// </summary>
    private async UniTask<int> LoadAllTablesAsync(CancellationToken cancellationToken)
    {
        int loadedTableCount = 0;

        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            try
            {
                // Reflection으로 CSVParser.ParseAsync<T>()를 호출합니다.
                MethodInfo method = typeof(CSVParser).GetMethod("ParseAsync",
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    GameLogger.LogError("[CSVManager] ParseAsync 메서드를 찾을 수 없음");
                    continue;
                }

                MethodInfo genericMethod = method.MakeGenericMethod(type);
                object[] parameters = new object[] { tableName, cancellationToken, CSVParser.ParseMode.Lenient };
                object task = genericMethod.Invoke(null, parameters);

                if (task == null)
                {
                    GameLogger.LogError($"[CSVManager] ParseAsync 호출 실패: {tableName}");
                    continue;
                }

                Type taskType = task.GetType();
                MethodInfo getAwaiterMethod = taskType.GetMethod("GetAwaiter");

                if (getAwaiterMethod != null)
                {
                    object awaiter = getAwaiterMethod.Invoke(task, null);
                    Type awaiterType = awaiter.GetType();

                    // Reflection 기반 UniTask 결과를 안전하게 회수합니다.
                    while (true)
                    {
                        PropertyInfo isCompletedProp = awaiterType.GetProperty("IsCompleted");
                        bool isCompleted = (bool)isCompletedProp.GetValue(awaiter);

                        if (isCompleted)
                        {
                            MethodInfo getResultMethod = awaiterType.GetMethod("GetResult");
                            object result = getResultMethod.Invoke(awaiter, null);
                            tables[type] = result;
                            loadedTableCount++;
                            break;
                        }

                        await UniTask.Yield();
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 테이블 로드 실패: {tableName}\n{e.Message}\n{e.StackTrace}");
            }
        }

        return loadedTableCount;
    }

    /// <summary>
    /// 모든 참조를 해결하고 성공 개수를 반환합니다.
    /// </summary>
    private int ResolveAllReferences()
    {
        Dictionary<string, object> tableMap = new Dictionary<string, object>();
        int resolvedReferenceCount = 0;

        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            if (tables.ContainsKey(type))
            {
                tableMap[tableName] = tables[type];
            }
        }

        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];

            if (!tables.ContainsKey(type))
                continue;

            try
            {
                MethodInfo method = typeof(CSVReferenceResolver).GetMethod("ResolveReferences",
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    GameLogger.LogError("[CSVManager] ResolveReferences 메서드를 찾을 수 없음");
                    continue;
                }

                MethodInfo genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(null, new object[] { tables[type], tableMap });
                resolvedReferenceCount++;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 참조 해결 실패: {type.Name}\n{e.Message}");
            }
        }

        return resolvedReferenceCount;
    }

    /// <summary>
    /// 타입으로부터 테이블명을 추출합니다.
    /// </summary>
    private string GetTableName(Type type)
    {
        CSVTableAttribute attr = type.GetCustomAttribute<CSVTableAttribute>();
        if (attr != null)
            return attr.TableName;

        return type.Name;
    }

    /// <summary>
    /// 특정 타입의 테이블을 반환합니다.
    /// </summary>
    public List<T> GetTable<T>() where T : ICSVData
    {
        if (tables.TryGetValue(typeof(T), out object table))
            return (List<T>)table;

        GameLogger.LogWarning($"[CSVManager] 테이블 없음: {typeof(T).Name}");
        return new List<T>();
    }

    /// <summary>
    /// 특정 테이블의 스키마를 반환합니다.
    /// </summary>
    public CSVSchema GetSchema(string tableName)
    {
        if (schemas.TryGetValue(tableName, out CSVSchema schema))
            return schema;

        GameLogger.LogWarning($"[CSVManager] 스키마 없음: {tableName}");
        return null;
    }

    /// <summary>
    /// 로드된 모든 테이블 타입을 반환합니다.
    /// </summary>
    public List<Type> GetAllTableTypes()
    {
        return new List<Type>(csvDataTypes);
    }
}
