using System;
using TMPro;
using UnityEngine;

/// <summary>
/// TMP_Text에 자동으로 번역된 텍스트를 표시하는 컴포넌트
/// 인스펙터에서 Key 입력 시 자동으로 해당 언어의 텍스트 표시
/// 에디터 모드에서도 즉시 미리보기 지원
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways] // 에디터 모드에서도 실행
public class LocalizedText : MonoBehaviour
{
    [SerializeField]
    [Tooltip("로컬라이징 키 (예: UI_BTN_START)")]
    private string key;

    private TMP_Text text;

#if UNITY_EDITOR
    private string lastValidatedKey;
#endif

    /// <summary>
    /// 로컬라이징 키
    /// </summary>
    public string Key
    {
        get => key;
        set
        {
            key = value;
            UpdateText();
        }
    }

    private void Awake()
    {
        text = GetComponent<TMP_Text>();

        // 런타임에만 이벤트 구독
        if (Application.isPlaying)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            UpdateText();
            UpdateFont();
        }
    }

    private void OnDestroy()
    {
        // 런타임에만 이벤트 해제
        if (Application.isPlaying && LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 에디터 모드에서 Key 변경 시 즉시 텍스트 업데이트
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        // key가 실제로 변경되었을 때만 업데이트
        if (lastValidatedKey != key)
        {
            lastValidatedKey = key;

            if (text == null)
                text = GetComponent<TMP_Text>();

            UpdateText();
            UpdateFont();
        }
#endif
    }

    /// <summary>
    /// 언어 변경 시 호출되는 콜백
    /// </summary>
    private void OnLanguageChanged(LanguageType newLanguage)
    {
        UpdateText();
        UpdateFont();
    }

    /// <summary>
    /// 에디터와 런타임 환경에 따라 적절한 데이터를 가져오는 헬퍼 메서드
    /// </summary>
    private T GetLocalizationValue<T>(Func<T> runtimeGetter, Func<T> editorGetter) where T : class
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return editorGetter();
        }
#endif

        if (!LocalizationManager.Instance.IsInitialized)
            return null;

        return runtimeGetter();
    }

    /// <summary>
    /// 텍스트 업데이트 (에디터/런타임 모두 지원)
    /// </summary>
    private void UpdateText()
    {
        if (text == null || string.IsNullOrEmpty(key))
            return;

        string localizedText = GetLocalizationValue(
            () => LocalizationManager.Instance.GetText(key),
            () => LocalizationManager.Instance.GetTextInEditor(key)
        );

        if (localizedText != null)
        {
            text.text = localizedText;
        }
    }

    /// <summary>
    /// 포맷팅된 텍스트 설정 (런타임 전용)
    /// 예: "점수: {0}" → "점수: 1000"
    /// </summary>
    public void SetFormattedText(params object[] args)
    {
        if (text == null || string.IsNullOrEmpty(key))
            return;

        if (!Application.isPlaying)
            return;

        string localizedText = LocalizationManager.Instance.GetText(key, args);
        text.text = localizedText;
    }

    /// <summary>
    /// 강제로 텍스트 갱신 (수동 갱신 필요 시)
    /// </summary>
    public void RefreshText()
    {
        UpdateText();
        UpdateFont();
    }

    /// <summary>
    /// 현재 언어에 맞는 폰트 적용 (에디터/런타임 모두 지원)
    /// </summary>
    private void UpdateFont()
    {
        if (text == null)
            return;

        TMP_FontAsset font = GetLocalizationValue(
            () => LocalizationManager.Instance.GetCurrentFont(),
            () => LocalizationManager.Instance.GetCurrentFontInEditor()
        );

        if (font != null)
        {
            text.font = font;
        }
    }
}
