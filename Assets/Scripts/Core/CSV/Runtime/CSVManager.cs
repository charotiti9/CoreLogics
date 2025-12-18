using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Core.Utilities;

/// <summary>
/// 모든 CSV 데이터를 Generic하게 관리하는 싱글톤
/// </summary>
public class CSVManager : LazySingleton<CSVManager>
{
    private Dictionary<Type, object> tables = new Dictionary<Type, object>();
    private Dictionary<string, CSVSchema> schemas = new Dictionary<string, CSVSchema>();
    private List<Type> csvDataTypes = new List<Type>();

    /// <summary>
    /// 모든 CSV 로드 및 참조 해결
    /// </summary>
    /// <param name="cancellationToken">취소 토큰</param>
    public async UniTask Initialize(CancellationToken cancellationToken = default)
    {
        GameLogger.Log("[CSVManager] 초기화 시작");

        // 1. Assembly에서 ICSVData 타입 찾기
        FindAllCSVDataTypes();

        // 2. 모든 스키마 로드
        await LoadAllSchemasAsync(cancellationToken);

        // 3. 모든 데이터 로드
        await LoadAllTablesAsync(cancellationToken);

        // 4. 참조 해결
        ResolveAllReferences();

        GameLogger.Log("[CSVManager] 초기화 완료");
    }

    /// <summary>
    /// Assembly에서 ICSVData를 구현한 모든 타입 찾기
    /// </summary>
    private void FindAllCSVDataTypes()
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
                GameLogger.Log($"[CSVManager] CSV 타입 발견: {type.Name}");
            }
        }
    }

    /// <summary>
    /// 모든 스키마 로드
    /// </summary>
    private async UniTask LoadAllSchemasAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            try
            {
                CSVSchema schema = await CSVSchemaParser.ParseSchemaAsync(tableName, cancellationToken);
                schemas[tableName] = schema;
                GameLogger.Log($"[CSVManager] 스키마 로드 완료: {tableName}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 스키마 로드 실패: {tableName}\n{e.Message}");
            }
        }
    }

    /// <summary>
    /// 모든 테이블 로드 (Generic, Reflection 사용)
    /// </summary>
    private async UniTask LoadAllTablesAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            try
            {
                // CSVParser.ParseAsync<T>를 Reflection으로 호출
                MethodInfo method = typeof(CSVParser).GetMethod("ParseAsync",
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    GameLogger.LogError($"[CSVManager] ParseAsync 메서드를 찾을 수 없음");
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

                // UniTask<List<T>> await
                Type taskType = task.GetType();
                MethodInfo getAwaiterMethod = taskType.GetMethod("GetAwaiter");

                if (getAwaiterMethod != null)
                {
                    object awaiter = getAwaiterMethod.Invoke(task, null);
                    Type awaiterType = awaiter.GetType();

                    // GetResult로 결과 가져오기 (동기적으로 대기)
                    while (true)
                    {
                        PropertyInfo isCompletedProp = awaiterType.GetProperty("IsCompleted");
                        bool isCompleted = (bool)isCompletedProp.GetValue(awaiter);

                        if (isCompleted)
                        {
                            MethodInfo getResultMethod = awaiterType.GetMethod("GetResult");
                            object result = getResultMethod.Invoke(awaiter, null);
                            tables[type] = result;
                            break;
                        }

                        await UniTask.Yield();
                    }
                }

                GameLogger.Log($"[CSVManager] 테이블 로드 완료: {tableName}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 테이블 로드 실패: {tableName}\n{e.Message}\n{e.StackTrace}");
            }
        }
    }

    /// <summary>
    /// 모든 참조 해결 (Generic)
    /// </summary>
    private void ResolveAllReferences()
    {
        // 테이블명 → 데이터 매핑
        Dictionary<string, object> tableMap = new Dictionary<string, object>();

        for (int i = 0; i < csvDataTypes.Count; i++)
        {
            Type type = csvDataTypes[i];
            string tableName = GetTableName(type);

            if (tables.ContainsKey(type))
            {
                tableMap[tableName] = tables[type];
            }
        }

        // 각 테이블의 참조 해결
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
                    GameLogger.LogError($"[CSVManager] ResolveReferences 메서드를 찾을 수 없음");
                    continue;
                }

                MethodInfo genericMethod = method.MakeGenericMethod(type);
                genericMethod.Invoke(null, new object[] { tables[type], tableMap });

                GameLogger.Log($"[CSVManager] 참조 해결 완료: {type.Name}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CSVManager] 참조 해결 실패: {type.Name}\n{e.Message}");
            }
        }
    }

    /// <summary>
    /// 타입으로부터 테이블명 추출
    /// </summary>
    private string GetTableName(Type type)
    {
        CSVTableAttribute attr = type.GetCustomAttribute<CSVTableAttribute>();
        if (attr != null)
            return attr.TableName;

        return type.Name;
    }

    /// <summary>
    /// 특정 타입의 테이블 가져오기
    /// </summary>
    public List<T> GetTable<T>() where T : ICSVData
    {
        if (tables.TryGetValue(typeof(T), out object table))
            return (List<T>)table;

        GameLogger.LogWarning($"[CSVManager] 테이블 없음: {typeof(T).Name}");
        return new List<T>();
    }

    /// <summary>
    /// 특정 테이블의 스키마 가져오기
    /// </summary>
    public CSVSchema GetSchema(string tableName)
    {
        if (schemas.TryGetValue(tableName, out CSVSchema schema))
            return schema;

        GameLogger.LogWarning($"[CSVManager] 스키마 없음: {tableName}");
        return null;
    }

    /// <summary>
    /// 로드된 모든 테이블 타입 반환
    /// </summary>
    public List<Type> GetAllTableTypes()
    {
        return new List<Type>(csvDataTypes);
    }

}
