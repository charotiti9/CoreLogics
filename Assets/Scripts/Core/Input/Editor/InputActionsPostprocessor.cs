#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// InputSystem_Actions.inputactions 파일 변경 감지 및 자동 재생성
/// </summary>
public class InputActionsPostprocessor : AssetPostprocessor
{
    private const string INPUT_ACTIONS_FILE = "InputSystem_Actions.inputactions";

    /// <summary>
    /// 에셋이 임포트/수정/삭제될 때 호출
    /// </summary>
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        // .inputactions 파일이 변경되었는지 확인
        bool needsRegenerate = false;

        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(INPUT_ACTIONS_FILE))
            {
                needsRegenerate = true;
                Debug.Log($"[InputManager] InputActions 파일 변경 감지: {assetPath}");
                break;
            }
        }

        // 자동 재생성
        if (needsRegenerate)
        {
            // 약간의 지연 후 재생성 (Unity가 파일을 완전히 임포트할 때까지 대기)
            EditorApplication.delayCall += () =>
            {
                InputActionCodeGenerator.GenerateCode();
            };
        }
    }
}
#endif
