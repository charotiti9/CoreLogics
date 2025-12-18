using System.Threading;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using Core.Addressable;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 로컬라이징 폰트 제공자
/// LocalizationSettings 로딩 및 폰트 제공 전담
/// </summary>
public class LocalizationFontProvider
{
    private const string SETTINGS_KEY = "Assets/Data/Settings/LocalizationSettings.asset";

    private LocalizationSettings settings;
    private bool isLoaded;

    /// <summary>
    /// 설정 로드 완료 여부
    /// </summary>
    public bool IsLoaded => isLoaded;

    /// <summary>
    /// 생성자
    /// </summary>
    public LocalizationFontProvider()
    {
        settings = null;
        isLoaded = false;
    }

    /// <summary>
    /// Addressable을 통해 LocalizationSettings 로드
    /// </summary>
    public async UniTask LoadSettingsAsync(CancellationToken cancellationToken)
    {
        if (isLoaded)
            return;

        var handle = await AddressableLoader.Instance.LoadAssetAsync<LocalizationSettings>(
            SETTINGS_KEY,
            cancellationToken
        );

        settings = handle;

        if (settings == null)
        {
            Debug.LogError("[LocalizationFontProvider] LocalizationSettings 로드 실패!");
            isLoaded = false;
        }
        else
        {
            Debug.Log("[LocalizationFontProvider] LocalizationSettings 로드 완료");
            isLoaded = true;
        }
    }

    /// <summary>
    /// 언어에 맞는 폰트 반환
    /// </summary>
    public TMP_FontAsset GetFont(LanguageType language)
    {
        if (settings == null)
        {
            Debug.LogWarning("[LocalizationFontProvider] LocalizationSettings가 로드되지 않았습니다.");
            return null;
        }

        return settings.GetFont(language);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 전용: AssetDatabase로 직접 로드하여 폰트 반환
    /// LocalizedText 컴포넌트의 에디터 미리보기용
    /// </summary>
    public TMP_FontAsset GetFontInEditor(LanguageType language)
    {
        // 에디터 모드에서 AssetDatabase로 직접 로드
        string settingsPath = "Assets/Data/Settings/LocalizationSettings.asset";
        var editorSettings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(settingsPath);

        if (editorSettings == null)
        {
            Debug.LogWarning($"[LocalizationFontProvider] 에디터에서 설정 파일을 찾을 수 없음: {settingsPath}");
            return null;
        }

        return editorSettings.GetFont(language);
    }
#endif
}
