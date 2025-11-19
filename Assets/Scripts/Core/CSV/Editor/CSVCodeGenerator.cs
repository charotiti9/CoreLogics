#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// CSV 스키마로부터 C# 클래스 자동 생성 (Dirty 체크 포함)
/// </summary>
public static class CSVCodeGenerator
{
    private const string CSV_ROOT_PATH = "Assets/Data/CSV";
    private const string CODE_OUTPUT_PATH = "Assets/Scripts/Data/Generated";
    private const string ADDRESSABLES_GROUP_NAME = "CSV Data";

    /// <summary>
    /// 변경된 CSV만 선택적으로 재생성 (Dirty Check)
    /// </summary>
    public static void GenerateChangedClasses()
    {
        if (!Directory.Exists(CSV_ROOT_PATH))
        {
            Debug.LogError($"[CSVCodeGenerator] CSV 폴더 없음: {CSV_ROOT_PATH}");
            return;
        }

        // 모든 _Schema.csv 파일 찾기
        string[] schemaFiles = Directory.GetFiles(CSV_ROOT_PATH, "*_Schema.csv", SearchOption.TopDirectoryOnly);

        int generatedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < schemaFiles.Length; i++)
        {
            string schemaPath = schemaFiles[i];
            string fileName = Path.GetFileNameWithoutExtension(schemaPath); // "ItemData_Schema"

            // "ItemData_Schema" → "ItemData"
            string tableName = fileName.Replace("_Schema", "");

            // Dirty 체크
            string csFilePath = Path.Combine(CODE_OUTPUT_PATH, $"{tableName}.cs");

            if (IsDirty(schemaPath, csFilePath))
            {
                Debug.Log($"[CSVCodeGenerator] 변경 감지: {tableName}");
                GenerateClassFromSchema(schemaPath, tableName);
                generatedCount++;
            }
            else
            {
                Debug.Log($"[CSVCodeGenerator] 변경 없음: {tableName} (스킵)");
                skippedCount++;
            }
        }

        if (generatedCount > 0)
        {
            AssetDatabase.Refresh();
        }

        // CSV 파일들을 자동으로 Addressables에 등록
        SetupAddressables();

        Debug.Log($"[CSVCodeGenerator] 완료 - 생성: {generatedCount}, 스킵: {skippedCount}");
    }

    /// <summary>
    /// CSV 폴더의 모든 파일을 Addressables에 자동 등록
    /// </summary>
    private static void SetupAddressables()
    {
        // Addressables Settings 가져오기 (없으면 자동 생성)
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

        if (settings == null)
        {
            Debug.LogError("[CSVCodeGenerator] Addressables 설정 생성 실패");
            return;
        }

        // CSV Data 그룹 찾기 또는 생성
        AddressableAssetGroup group = settings.FindGroup(ADDRESSABLES_GROUP_NAME);
        if (group == null)
        {
            group = settings.CreateGroup(ADDRESSABLES_GROUP_NAME, false, false, true, null);
            Debug.Log($"[CSVCodeGenerator] Addressables 그룹 생성: {ADDRESSABLES_GROUP_NAME}");
        }

        // CSV 폴더의 모든 .csv 파일 찾기
        string[] csvFiles = Directory.GetFiles(CSV_ROOT_PATH, "*.csv", SearchOption.TopDirectoryOnly);

        int addedCount = 0;
        int existingCount = 0;

        for (int i = 0; i < csvFiles.Length; i++)
        {
            string csvPath = csvFiles[i].Replace("\\", "/");
            string guid = AssetDatabase.AssetPathToGUID(csvPath);

            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[CSVCodeGenerator] GUID를 찾을 수 없음: {csvPath}");
                continue;
            }

            // 이미 Addressable로 등록되어 있는지 확인
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry == null)
            {
                // 새로 등록
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = csvPath; // 주소를 파일 경로로 설정
                addedCount++;
                Debug.Log($"[CSVCodeGenerator] Addressable 등록: {csvPath}");
            }
            else
            {
                existingCount++;
            }
        }

        if (addedCount > 0)
        {
            // 변경사항 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CSVCodeGenerator] Addressables 설정 완료 - 추가: {addedCount}, 기존: {existingCount}");
        }
        else
        {
            Debug.Log($"[CSVCodeGenerator] 모든 CSV 파일이 이미 Addressable로 등록되어 있습니다. (총 {existingCount}개)");
        }
    }

    /// <summary>
    /// Dirty 체크: Schema CSV가 C# 파일보다 최신인가?
    /// </summary>
    private static bool IsDirty(string schemaPath, string csFilePath)
    {
        if (!File.Exists(csFilePath))
            return true;

        System.DateTime schemaTime = File.GetLastWriteTime(schemaPath);
        System.DateTime csTime = File.GetLastWriteTime(csFilePath);

        return schemaTime > csTime;
    }

    /// <summary>
    /// Schema CSV 파일로부터 C# 클래스 생성
    /// </summary>
    private static void GenerateClassFromSchema(string schemaPath, string tableName)
    {
        // Schema CSV 파싱 (간단한 수동 파싱)
        List<CSVSchemaColumn> columns = ParseSchemaCSV(schemaPath);

        if (columns.Count == 0)
        {
            Debug.LogError($"[CSVCodeGenerator] 스키마 비어있음: {tableName}");
            return;
        }

        CSVSchema schema = new CSVSchema
        {
            TableName = tableName,
            Columns = columns
        };

        GenerateClass(schema);
    }

    /// <summary>
    /// Schema CSV를 수동으로 파싱 (Editor 전용, 간단한 파싱)
    /// </summary>
    private static List<CSVSchemaColumn> ParseSchemaCSV(string filePath)
    {
        List<CSVSchemaColumn> columns = new List<CSVSchemaColumn>();

        if (!File.Exists(filePath))
        {
            Debug.LogError($"[CSVCodeGenerator] 파일 없음: {filePath}");
            return columns;
        }

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length < 2)
            return columns;

        // 헤더 스킵, 데이터만 읽기
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (string.IsNullOrEmpty(line))
                continue;

            string[] values = line.Split(',');

            if (values.Length < 4)
                continue;

            CSVSchemaColumn column = new CSVSchemaColumn
            {
                ColumnName = values[0].Trim(),
                Type = values[1].Trim(),
                Description = values[2].Trim(),
                Reference = values[3].Trim()
            };

            columns.Add(column);
        }

        return columns;
    }

    /// <summary>
    /// C# 클래스 생성
    /// </summary>
    private static void GenerateClass(CSVSchema schema)
    {
        StringBuilder sb = new StringBuilder();

        // 헤더
        sb.AppendLine($"// Auto-Generated from {schema.TableName}_Schema.csv");
        sb.AppendLine("// 수정하지 마세요!");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine();

        // 클래스 선언
        sb.AppendLine("[Serializable]");
        sb.AppendLine($"[CSVTable(\"{schema.TableName}\")]");
        sb.AppendLine($"public class {schema.TableName} : ICSVData");
        sb.AppendLine("{");

        // 필드 생성
        for (int i = 0; i < schema.Columns.Count; i++)
        {
            CSVSchemaColumn column = schema.Columns[i];

            // 주석
            if (!string.IsNullOrEmpty(column.Description))
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {column.Description}");
                sb.AppendLine("    /// </summary>");
            }

            // 참조 필드 처리
            if (column.HasReference)
            {
                sb.AppendLine($"    [CSVReference(\"{column.ReferenceTableName}\", \"{column.ReferenceColumnName}\")]");

                // 참조 객체 필드 (CategoryID → Category)
                string refFieldName = column.ColumnName.Replace("ID", "");
                sb.AppendLine($"    public {column.ReferenceTableName} {refFieldName};");
                sb.AppendLine();

                // ID 필드 (내부용)
                sb.AppendLine($"    public {ConvertToCSType(column.Type)} {column.ColumnName};");
            }
            else
            {
                // 일반 필드
                string csType = ConvertToCSType(column.Type);
                sb.AppendLine($"    public {csType} {column.ColumnName};");
            }

            sb.AppendLine();
        }

        sb.AppendLine("}");

        // 파일 저장
        string filePath = Path.Combine(CODE_OUTPUT_PATH, $"{schema.TableName}.cs");
        Directory.CreateDirectory(CODE_OUTPUT_PATH);
        File.WriteAllText(filePath, sb.ToString());

        Debug.Log($"[CSVCodeGenerator] 생성 완료: {filePath}");
    }

    /// <summary>
    /// CSV 타입을 C# 타입으로 변환
    /// </summary>
    private static string ConvertToCSType(string csvType)
    {
        string lower = csvType.ToLower();

        if (lower == "int") return "int";
        if (lower == "float") return "float";
        if (lower == "bool") return "bool";
        if (lower == "string") return "string";
        if (lower == "int?") return "int?";
        if (lower == "float?") return "float?";
        if (lower == "bool?") return "bool?";

        return "string";
    }
}
#endif
