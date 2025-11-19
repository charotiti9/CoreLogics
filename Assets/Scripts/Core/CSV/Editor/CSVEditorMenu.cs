#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// CSV 관련 에디터 메뉴
/// </summary>
public static class CSVEditorMenu
{
    [MenuItem("Tools/CSV/Generate Scripts (Dirty Check)")]
    public static void GenerateScripts()
    {
        Debug.Log("[CSV] 스크립트 생성 시작 (Dirty Check)");
        CSVCodeGenerator.GenerateChangedClasses();
    }

    [MenuItem("Tools/CSV/Force Regenerate All")]
    public static void ForceRegenerateAll()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "전체 재생성",
            "모든 CSV 클래스를 강제로 재생성하시겠습니까?",
            "예",
            "아니오");

        if (confirm)
        {
            Debug.Log("[CSV] 전체 재생성 시작");

            string codePath = "Assets/Scripts/Data/Generated";

            if (System.IO.Directory.Exists(codePath))
            {
                System.IO.Directory.Delete(codePath, true);
            }

            AssetDatabase.Refresh();
            CSVCodeGenerator.GenerateChangedClasses();
        }
    }
}
#endif
