#if UNITY_EDITOR
using System;
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

        // 1단계: 모든 스키마 파싱
        Dictionary<string, CSVSchema> schemas = new Dictionary<string, CSVSchema>();

        for (int i = 0; i < schemaFiles.Length; i++)
        {
            string schemaPath = schemaFiles[i];
            string fileName = Path.GetFileNameWithoutExtension(schemaPath); // "ItemData_Schema"
            string tableName = fileName.Replace("_Schema", ""); // "ItemData"

            List<CSVSchemaColumn> columns = ParseSchemaCSV(schemaPath);
            if (columns.Count == 0)
            {
                Debug.LogWarning($"[CSVCodeGenerator] 스키마 비어있음: {tableName}");
                continue;
            }

            CSVSchema schema = new CSVSchema
            {
                TableName = tableName,
                Columns = columns
            };

            schemas[tableName] = schema;
        }

        // 2단계: 순환 참조 검사
        Debug.Log("[CSVCodeGenerator] 순환 참조 검사 시작");

        var graph = CSVCircularReferenceChecker.BuildReferenceGraph(schemas);

        if (CSVCircularReferenceChecker.HasCircularReference(graph, out List<string> cycle))
        {
            string cyclePath = CSVCircularReferenceChecker.FormatCyclePath(cycle);
            string errorMsg = $"[CSVCodeGenerator] 순환 참조 감지!\n순환 경로: {cyclePath}\n\n" +
                              $"테이블 간 참조가 순환을 이루고 있습니다. 스키마를 수정하여 순환을 제거해주세요.";

            Debug.LogError(errorMsg);
            EditorUtility.DisplayDialog(
                "CSV 순환 참조 감지",
                $"순환 경로: {cyclePath}\n\n테이블 간 참조가 순환을 이루고 있습니다.\n스키마를 수정하여 순환을 제거해주세요.",
                "확인");
            return; // 코드 생성 중단
        }

        Debug.Log("[CSVCodeGenerator] 순환 참조 검사 통과");

        // 3단계: 변경된 스키마만 코드 생성 (Dirty Check)
        int generatedCount = 0;
        int skippedCount = 0;

        for (int i = 0; i < schemaFiles.Length; i++)
        {
            string schemaPath = schemaFiles[i];
            string fileName = Path.GetFileNameWithoutExtension(schemaPath); // "ItemData_Schema"
            string tableName = fileName.Replace("_Schema", ""); // "ItemData"

            // Dirty 체크
            string csFilePath = Path.Combine(CODE_OUTPUT_PATH, $"{tableName}.cs");

            if (IsDirty(schemaPath, csFilePath))
            {
                Debug.Log($"[CSVCodeGenerator] 변경 감지: {tableName}");

                // 이미 파싱된 스키마 사용
                if (schemas.TryGetValue(tableName, out CSVSchema schema))
                {
                    GenerateClass(schema);
                    generatedCount++;
                }
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

        // 고아 파일 검사 (CSV는 삭제되었지만 .cs 파일만 남아있는 경우)
        List<string> orphanedFiles = DetectOrphanedClasses();
        if (orphanedFiles.Count > 0)
        {
            DeleteOrphanedClasses(orphanedFiles);
        }
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
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
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
    /// 기본 타입, Nullable, 배열, 리스트, 딕셔너리, 커스텀 타입 지원
    /// </summary>
    private static string ConvertToCSType(string csvType)
    {
        string trimmed = csvType.Trim();
        string lower = trimmed.ToLower();

        // 기본 타입
        if (lower == "int") return "int";
        if (lower == "float") return "float";
        if (lower == "bool") return "bool";
        if (lower == "string") return "string";

        // Nullable 타입
        if (lower == "int?") return "int?";
        if (lower == "float?") return "float?";
        if (lower == "bool?") return "bool?";

        // 배열 타입 (예: int[], string[], float[])
        if (trimmed.EndsWith("[]"))
        {
            return trimmed; // 그대로 반환
        }

        // 리스트 타입 (예: List<int>, List<string>)
        if (trimmed.StartsWith("List<", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(">"))
        {
            return trimmed; // 그대로 반환
        }

        // 딕셔너리 타입 (예: Dictionary<int,string>, Dictionary<string, int>)
        if (trimmed.StartsWith("Dictionary<", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(">"))
        {
            // 공백 정규화: "Dictionary<int,string>" → "Dictionary<int, string>"
            string normalized = NormalizeDictionaryType(trimmed);
            return normalized;
        }

        // Enum 및 커스텀 타입 (예: ItemType, Vector3, CustomClass)
        // 첫 글자가 대문자면 커스텀 타입으로 간주
        if (char.IsUpper(trimmed[0]))
        {
            return trimmed;
        }

        // 인식 불가능한 타입은 string으로 폴백
        Debug.LogWarning($"[CSVCodeGenerator] 알 수 없는 타입: '{csvType}' → string으로 처리됩니다.");
        return "string";
    }

    /// <summary>
    /// Dictionary 타입 문자열 정규화
    /// "Dictionary<int,string>" → "Dictionary<int, string>"
    /// </summary>
    private static string NormalizeDictionaryType(string dictType)
    {
        // "Dictionary<int,string>" 형식 파싱
        int openBracket = dictType.IndexOf('<');
        int closeBracket = dictType.LastIndexOf('>');

        if (openBracket < 0 || closeBracket < 0)
            return dictType;

        string innerTypes = dictType.Substring(openBracket + 1, closeBracket - openBracket - 1);
        string[] types = innerTypes.Split(',');

        if (types.Length == 2)
        {
            string keyType = types[0].Trim();
            string valueType = types[1].Trim();
            return $"Dictionary<{keyType}, {valueType}>";
        }

        return dictType;
    }

    /// <summary>
    /// CSV Schema가 삭제되었지만 생성된 .cs 파일만 남아있는 고아 파일 검사
    /// </summary>
    private static List<string> DetectOrphanedClasses()
    {
        List<string> orphanedFiles = new List<string>();

        if (!Directory.Exists(CODE_OUTPUT_PATH))
            return orphanedFiles;

        // 생성된 모든 .cs 파일 찾기
        string[] csFiles = Directory.GetFiles(CODE_OUTPUT_PATH, "*.cs", SearchOption.TopDirectoryOnly);

        foreach (string csFile in csFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(csFile); // "ItemData"
            string expectedSchemaPath = Path.Combine(CSV_ROOT_PATH, $"{fileName}_Schema.csv");

            // 대응하는 Schema가 없으면 고아 파일
            if (!File.Exists(expectedSchemaPath))
            {
                orphanedFiles.Add(csFile);
            }
        }

        return orphanedFiles;
    }

    /// <summary>
    /// 고아 파일 삭제 (사용자 확인 후)
    /// </summary>
    private static void DeleteOrphanedClasses(List<string> orphanedFiles)
    {
        if (orphanedFiles.Count == 0)
            return;

        // 파일 목록 생성
        StringBuilder fileList = new StringBuilder();
        fileList.AppendLine("다음 파일들은 대응하는 CSV Schema가 없습니다:");
        fileList.AppendLine();

        foreach (string filePath in orphanedFiles)
        {
            string fileName = Path.GetFileName(filePath);
            fileList.AppendLine($"  - {fileName}");
        }

        fileList.AppendLine();
        fileList.AppendLine("삭제하시겠습니까?");

        // 사용자 확인
        bool confirm = EditorUtility.DisplayDialog(
            "삭제된 CSV 감지",
            fileList.ToString(),
            "삭제",
            "취소");

        if (confirm)
        {
            // 파일 삭제
            int deletedCount = 0;
            foreach (string filePath in orphanedFiles)
            {
                try
                {
                    // .cs 파일 삭제
                    File.Delete(filePath);

                    // .meta 파일도 삭제
                    string metaPath = filePath + ".meta";
                    if (File.Exists(metaPath))
                    {
                        File.Delete(metaPath);
                    }

                    deletedCount++;
                    Debug.Log($"[CSVCodeGenerator] 고아 파일 삭제: {Path.GetFileName(filePath)}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CSVCodeGenerator] 파일 삭제 실패: {Path.GetFileName(filePath)} - {e.Message}");
                }
            }

            if (deletedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[CSVCodeGenerator] 고아 파일 삭제 완료: {deletedCount}개");
            }
        }
        else
        {
            Debug.Log("[CSVCodeGenerator] 고아 파일 삭제 취소됨");
        }
    }
}
#endif
