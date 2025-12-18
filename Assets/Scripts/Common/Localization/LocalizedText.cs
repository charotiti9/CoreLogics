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
    [Header("Localization")]
    [SerializeField]
    [Tooltip("로컬라이징 키 (예: UI_BTN_START)")]
    private string key;

    private TMP_Text text;

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
        if (text == null)
            text = GetComponent<TMP_Text>();

        UpdateText();
        UpdateFont();
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
    /// 텍스트 업데이트 (에디터/런타임 모두 지원)
    /// </summary>
    private void UpdateText()
    {
        if (text == null || string.IsNullOrEmpty(key))
            return;

        string localizedText;

#if UNITY_EDITOR
        // 에디터 모드에서는 동기적으로 CSV 직접 로드
        if (!Application.isPlaying)
        {
            localizedText = LocalizationManager.Instance.GetTextInEditor(key);
        }
        else
        {
            // 런타임에는 정상적으로 조회
            localizedText = LocalizationManager.Instance.GetText(key);
        }
#else
        // 빌드 모드에서는 항상 런타임 조회
        localizedText = LocalizationManager.Instance.GetText(key);
#endif

        text.text = localizedText;
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

        TMP_FontAsset font;

#if UNITY_EDITOR
        // 에디터 모드에서는 동기적으로 폰트 로드
        if (!Application.isPlaying)
        {
            font = LocalizationManager.Instance.GetCurrentFontInEditor();
        }
        else
        {
            // 런타임에는 정상적으로 조회
            font = LocalizationManager.Instance.GetCurrentFont();
        }
#else
        // 빌드 모드에서는 항상 런타임 조회
        font = LocalizationManager.Instance.GetCurrentFont();
#endif

        if (font != null)
        {
            text.font = font;
        }
    }
}
