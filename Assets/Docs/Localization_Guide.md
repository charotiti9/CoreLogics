# Localization 시스템 사용 가이드

## 개요

Localization 시스템은 게임의 다국어를 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ CSV 기반 간편한 번역 관리
- ✅ 에디터 실시간 미리보기 지원
- ✅ 자동 시스템 언어 감지
- ✅ 포맷팅 지원 (동적 값 삽입)
- ✅ 언어 변경 시 자동 UI 갱신
- ✅ LocalizedText 컴포넌트로 쉬운 UI 통합

---

## 핵심 개념

### 1. 3개 레이어 시스템

```
1. LocalizationManager (중앙 관리자)
   ↓
2. LocalizationData (CSV 데이터)
   ↓
3. LocalizedText (UI 컴포넌트)
```

**각 레이어의 역할:**
- **LocalizationManager**: 언어 설정 및 텍스트 조회
- **LocalizationData**: CSV에서 자동 생성된 데이터 클래스
- **LocalizedText**: TMP_Text에 자동으로 번역 텍스트 표시

### 2. 지원 언어

```csharp
public enum LanguageType
{
    Korean,   // 한국어
    English   // 영어

    // 필요 시 추가 가능
    // Japanese,
    // Chinese,
}
```

**언어 추가 방법:**
1. `LanguageType` enum에 언어 추가
2. CSV 파일에 해당 언어 컬럼 추가
3. LocalizationData 클래스 재생성

### 3. CSV 파일 구조

CSV 파일: `Assets/Data/CSV/LocalizationData.csv`

```csv
Key,Korean,English
UI_BTN_START,시작,Start
UI_BTN_OPTIONS,옵션,Options
UI_SCORE,점수: {0},Score: {0}
UI_LEVEL_INFO,레벨 {0} - 경험치 {1}/{2},Level {0} - EXP {1}/{2}
```

**컬럼 설명:**
- **Key**: 고유 식별자 (예: UI_BTN_START)
- **Korean**: 한국어 번역
- **English**: 영어 번역
- `{0}`, `{1}` 등은 런타임에 동적 값으로 대체됨

### 4. 자동 언어 감지

게임 최초 실행 시 시스템 언어를 감지합니다.

```csharp
SystemLanguage.Korean → LanguageType.Korean
SystemLanguage.English → LanguageType.English
기타 언어 → LanguageType.English (기본값)
```

언어 설정은 PlayerPrefs에 자동 저장됩니다.

---

## 기본 사용법

### 0. 초기 설정 (최초 1회)

로컬라이징 시스템을 사용하기 전에 폰트 설정 파일을 생성해야 합니다.

**Unity 에디터에서:**
1. Project 창에서 `Assets/Data/Settings/` 폴더 생성 (없다면)
2. 우클릭 → Create → Game → LocalizationSettings
3. Inspector에서 Language Fonts 섹션 확인
4. Korean Font에 한국어 TMP 폰트 할당
5. English Font에 영어 TMP 폰트 할당
6. Window → Asset Management → Addressables → Groups
7. LocalizationSettings.asset을 Addressable Groups에 드래그
8. Address를 "LocalizationSettings"로 설정

**참고:** LocalizedText 컴포넌트는 언어 변경 시 자동으로 해당 언어의 폰트를 적용합니다.

### 1. LocalizedText 컴포넌트 사용 (UI)

가장 쉬운 방법입니다. TMP_Text에 LocalizedText 컴포넌트를 추가하면 자동으로 번역되고 폰트도 자동으로 적용됩니다.

**Unity 에디터에서:**
1. TMP_Text 컴포넌트가 있는 GameObject 선택
2. Add Component → LocalizedText
3. Inspector에서 Key 입력 (예: `UI_BTN_START`)
4. 에디터에서 즉시 미리보기 확인!

**장점:**
- 코드 작성 불필요
- 언어 변경 시 자동 갱신
- 언어별 폰트 자동 적용
- 에디터 실시간 미리보기

```csharp
// 별도 코드 작성 불필요
// Inspector에서 Key만 설정하면 자동으로 동작
// 언어 변경 시 텍스트와 폰트가 모두 자동으로 갱신됨
```

### 2. 코드에서 텍스트 조회

LocalizedText 컴포넌트를 사용하지 않고 직접 조회할 수 있습니다.

```csharp
using UnityEngine;

public class GameUI : MonoBehaviour
{
    private void Start()
    {
        // 기본 텍스트 조회
        string startText = LocalizationManager.Instance.GetText("UI_BTN_START");
        Debug.Log(startText); // "시작" 또는 "Start"
    }
}
```

### 3. 포맷팅 (동적 값 삽입)

변수를 포함한 텍스트를 표시할 수 있습니다.

**CSV 준비:**
```csv
Key,Korean,English
UI_SCORE,점수: {0},Score: {0}
```

**코드에서 사용:**
```csharp
public class ScoreUI : MonoBehaviour
{
    private void UpdateScore(int score)
    {
        // "점수: {0}" → "점수: 1000"
        string scoreText = LocalizationManager.Instance.GetText("UI_SCORE", score);
        Debug.Log(scoreText); // "점수: 1000" 또는 "Score: 1000"
    }
}
```

### 4. 언어 변경

```csharp
public class SettingsMenu : MonoBehaviour
{
    public void OnKoreanButtonClicked()
    {
        // 한국어로 변경
        LocalizationManager.Instance.SetLanguage(LanguageType.Korean);
        // 모든 LocalizedText 컴포넌트가 자동으로 갱신됨!
    }

    public void OnEnglishButtonClicked()
    {
        // 영어로 변경
        LocalizationManager.Instance.SetLanguage(LanguageType.English);
    }
}
```

### 5. 현재 언어 확인

```csharp
public class LanguageDisplay : MonoBehaviour
{
    private void Start()
    {
        LanguageType current = LocalizationManager.Instance.CurrentLanguage;
        Debug.Log($"현재 언어: {current}");
    }
}
```

### 6. 언어 변경 이벤트 구독

언어가 변경될 때 커스텀 동작을 수행할 수 있습니다.

```csharp
public class CustomUI : MonoBehaviour
{
    private void Start()
    {
        // 이벤트 구독
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(LanguageType newLanguage)
    {
        Debug.Log($"언어 변경됨: {newLanguage}");

        // 커스텀 UI 갱신 로직...
    }
}
```

---

## 실전 예제

### 메인 메뉴 버튼

**Unity 에디터 설정:**
1. Start Button (TMP_Text) → LocalizedText 컴포넌트 추가
   - Key: `UI_BTN_START`
2. Options Button (TMP_Text) → LocalizedText 컴포넌트 추가
   - Key: `UI_BTN_OPTIONS`
3. Quit Button (TMP_Text) → LocalizedText 컴포넌트 추가
   - Key: `UI_BTN_QUIT`

**CSV:**
```csv
Key,Korean,English
UI_BTN_START,시작,Start
UI_BTN_OPTIONS,옵션,Options
UI_BTN_QUIT,종료,Quit
```

코드 작성 불필요! 에디터에서 즉시 확인 가능!

### 점수 표시 (포맷팅)

**CSV:**
```csv
Key,Korean,English
UI_SCORE,점수: {0},Score: {0}
```

**코드:**
```csharp
using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TMP_Text scoreText;
    private int currentScore = 0;

    private void Start()
    {
        UpdateScore(0);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScore(currentScore);
    }

    private void UpdateScore(int score)
    {
        // 포맷팅된 텍스트 조회
        scoreText.text = LocalizationManager.Instance.GetText("UI_SCORE", score);
    }
}
```

### LocalizedText 컴포넌트에서 포맷팅 사용

**CSV:**
```csv
Key,Korean,English
UI_SCORE,점수: {0},Score: {0}
```

**Unity 에디터:**
1. TMP_Text에 LocalizedText 컴포넌트 추가
2. Key: `UI_SCORE`

**코드:**
```csharp
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private LocalizedText scoreText;

    public void UpdateScore(int score)
    {
        // SetFormattedText로 동적 값 전달
        scoreText.SetFormattedText(score);
    }
}
```

### 플레이어 정보 (복수 파라미터)

**CSV:**
```csv
Key,Korean,English
UI_PLAYER_INFO,플레이어: {0} | 레벨: {1},Player: {0} | Level: {1}
UI_LEVEL_DETAIL,레벨 {0} - 경험치 {1}/{2},Level {0} - EXP {1}/{2}
```

**코드:**
```csharp
using UnityEngine;
using TMPro;

public class PlayerInfoUI : MonoBehaviour
{
    public TMP_Text playerInfoText;
    public TMP_Text levelDetailText;

    public void UpdatePlayerInfo(string playerName, int level, int currentExp, int maxExp)
    {
        // 2개 파라미터
        playerInfoText.text = LocalizationManager.Instance.GetText(
            "UI_PLAYER_INFO",
            playerName,
            level
        );

        // 3개 파라미터
        levelDetailText.text = LocalizationManager.Instance.GetText(
            "UI_LEVEL_DETAIL",
            level,
            currentExp,
            maxExp
        );
    }
}

// 사용 예시
// UpdatePlayerInfo("홍길동", 5, 350, 500);
// → "플레이어: 홍길동 | 레벨: 5"
// → "레벨 5 - 경험치 350/500"
```

### 설정 메뉴 (언어 선택)

**CSV:**
```csv
Key,Korean,English
UI_SETTINGS,설정,Settings
UI_LANGUAGE,언어,Language
UI_LANG_KOREAN,한국어,Korean
UI_LANG_ENGLISH,영어,English
```

**코드:**
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text languageLabelText;
    public Button koreanButton;
    public Button englishButton;

    private void Start()
    {
        // 버튼 이벤트 등록
        koreanButton.onClick.AddListener(OnKoreanClicked);
        englishButton.onClick.AddListener(OnEnglishClicked);

        // 언어 변경 이벤트 구독
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

        // 초기 UI 갱신
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnKoreanClicked()
    {
        LocalizationManager.Instance.SetLanguage(LanguageType.Korean);
    }

    private void OnEnglishClicked()
    {
        LocalizationManager.Instance.SetLanguage(LanguageType.English);
    }

    private void OnLanguageChanged(LanguageType newLanguage)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // LocalizedText 컴포넌트가 없는 경우 수동 갱신
        titleText.text = LocalizationManager.Instance.GetText("UI_SETTINGS");
        languageLabelText.text = LocalizationManager.Instance.GetText("UI_LANGUAGE");
    }
}
```

**또는 LocalizedText 사용 (더 간단):**
```csharp
// titleText, languageLabelText에 LocalizedText 컴포넌트 추가
// Key만 설정하면 자동으로 갱신되므로 UpdateUI() 불필요!

public class SettingsMenu : MonoBehaviour
{
    public Button koreanButton;
    public Button englishButton;

    private void Start()
    {
        koreanButton.onClick.AddListener(OnKoreanClicked);
        englishButton.onClick.AddListener(OnEnglishClicked);
    }

    private void OnKoreanClicked()
    {
        LocalizationManager.Instance.SetLanguage(LanguageType.Korean);
    }

    private void OnEnglishClicked()
    {
        LocalizationManager.Instance.SetLanguage(LanguageType.English);
    }
}
```

### 아이템 설명 (동적 텍스트)

**CSV:**
```csv
Key,Korean,English
ITEM_POTION_NAME,회복 물약,Health Potion
ITEM_POTION_DESC,체력을 {0} 회복합니다.,Restores {0} HP.
ITEM_SWORD_NAME,강철 검,Steel Sword
ITEM_SWORD_DESC,공격력 +{0},Attack +{0}
```

**코드:**
```csharp
using UnityEngine;
using TMPro;

public class ItemTooltip : MonoBehaviour
{
    public TMP_Text itemNameText;
    public TMP_Text itemDescText;

    public void ShowPotion(int healAmount)
    {
        itemNameText.text = LocalizationManager.Instance.GetText("ITEM_POTION_NAME");
        itemDescText.text = LocalizationManager.Instance.GetText("ITEM_POTION_DESC", healAmount);

        // 한국어: "회복 물약" | "체력을  50 회복합니다."
        // 영어: "Health Potion" | "Restores 50 HP."
    }

    public void ShowSword(int attackPower)
    {
        itemNameText.text = LocalizationManager.Instance.GetText("ITEM_SWORD_NAME");
        itemDescText.text = LocalizationManager.Instance.GetText("ITEM_SWORD_DESC", attackPower);

        // 한국어: "강철 검" | "공격력 +30"
        // 영어: "Steel Sword" | "Attack +30"
    }
}
```

### 대화 시스템

**CSV:**
```csv
Key,Korean,English
DIALOG_NPC_GREETING,안녕하세요 모험가님!,Hello adventurer!
DIALOG_NPC_QUEST,"퀘스트를 수락하시겠습니까? 보상: {0} 골드",Will you accept the quest? Reward: {0} Gold
DIALOG_PLAYER_ACCEPT,수락합니다,Accept
DIALOG_PLAYER_DECLINE,거절합니다,Decline
```

**코드:**
```csharp
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogSystem : MonoBehaviour
{
    public TMP_Text dialogText;
    public Button acceptButton;
    public Button declineButton;
    public TMP_Text acceptButtonText;
    public TMP_Text declineButtonText;

    private void Start()
    {
        // 버튼 텍스트는 LocalizedText 컴포넌트 사용 권장
        // 여기서는 수동 갱신 예시
        UpdateButtonTexts();

        // 언어 변경 시 버튼 텍스트 갱신
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(LanguageType newLanguage)
    {
        UpdateButtonTexts();
    }

    private void UpdateButtonTexts()
    {
        acceptButtonText.text = LocalizationManager.Instance.GetText("DIALOG_PLAYER_ACCEPT");
        declineButtonText.text = LocalizationManager.Instance.GetText("DIALOG_PLAYER_DECLINE");
    }

    public void ShowGreeting()
    {
        dialogText.text = LocalizationManager.Instance.GetText("DIALOG_NPC_GREETING");
    }

    public void ShowQuest(int rewardGold)
    {
        dialogText.text = LocalizationManager.Instance.GetText("DIALOG_NPC_QUEST", rewardGold);
        // "퀘스트를 수락하시겠습니까? 보상: 100 골드"
    }
}
```

### 게임 시작 시 언어 초기화

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GameBootstrap : MonoBehaviour
{
    private CancellationTokenSource cts;

    private void Awake()
    {
        cts = new CancellationTokenSource();
        InitializeAsync(cts.Token).Forget();
    }

    private async UniTaskVoid InitializeAsync(CancellationToken cancellationToken)
    {
        // CSVManager 초기화 (LocalizationData 로드)
        await CSVManager.Instance.Initialize(cancellationToken);

        // LocalizationManager 비동기 초기화
        // 자동으로 PlayerPrefs에서 언어 로드 또는 시스템 언어 감지
        // LocalizationSettings.asset을 Addressable로 로드
        await LocalizationManager.Instance.InitializeLocalizeCSVAsync(cancellationToken);

        Debug.Log($"게임 시작 - 언어: {LocalizationManager.Instance.CurrentLanguage}");
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

---

## 고급 기능

### 언어별 폰트 자동 변경

LocalizationManager는 LocalizationSettings.asset을 통해 언어별 폰트를 관리합니다.

**초기 설정:**
1. Project 창에서 `Assets/Data/Settings/LocalizationSettings.asset` 선택
2. Inspector에서 Language Fonts 섹션 확인
3. Korean Font에 한국어 TMP 폰트 할당
4. English Font에 영어 TMP 폰트 할당
5. Addressable Groups에서 Address가 "LocalizationSettings"로 설정되어 있는지 확인

**LocalizedText 컴포넌트 사용:**
```csharp
// LocalizedText 컴포넌트를 추가하면 자동으로 처리됨
// 언어 변경 시 텍스트와 함께 폰트도 자동으로 변경됨
// 에디터 모드에서도 미리보기 지원!
```

**수동으로 폰트 가져오기 (런타임):**
```csharp
using UnityEngine;
using TMPro;

public class CustomTextComponent : MonoBehaviour
{
    private TMP_Text text;

    private void Start()
    {
        text = GetComponent<TMP_Text>();

        // 현재 언어에 맞는 폰트 가져오기 (런타임 전용)
        TMP_FontAsset currentFont = LocalizationManager.Instance.GetCurrentFont();
        if (currentFont != null)
        {
            text.font = currentFont;
        }

        // 언어 변경 이벤트 구독
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    private void OnLanguageChanged(LanguageType newLanguage)
    {
        // 새 언어에 맞는 폰트로 변경
        TMP_FontAsset newFont = LocalizationManager.Instance.GetCurrentFont();
        if (newFont != null)
        {
            text.font = newFont;
        }
    }
}
```

**에디터 전용 폰트 가져오기:**
```csharp
#if UNITY_EDITOR
using UnityEngine;
using TMPro;

public class EditorTextPreview : MonoBehaviour
{
    private void OnValidate()
    {
        var text = GetComponent<TMP_Text>();

        // 에디터 모드에서 폰트 미리보기
        TMP_FontAsset font = LocalizationManager.Instance.GetCurrentFontInEditor();
        if (font != null)
        {
            text.font = font;
        }
    }
}
#endif
```

**참고:**
- LocalizedText 컴포넌트를 사용하면 폰트 변경이 자동으로 처리되므로 위 코드를 작성할 필요가 없습니다.
- 커스텀 텍스트 컴포넌트를 만들 때만 위와 같이 수동으로 폰트를 변경하세요.
- 에디터 미리보기가 필요하면 `GetCurrentFontInEditor()`를 사용하세요.

### 복수형 처리

영어는 복수형이 있지만 한국어는 없는 경우입니다.

**CSV:**
```csv
Key,Korean,English
UI_ITEM_COUNT,아이템 {0}개,{0} Item(s)
UI_ITEM_SINGLE,아이템 1개,1 Item
UI_ITEM_PLURAL,아이템 {0}개,{0} Items
```

**코드:**
```csharp
using UnityEngine;

public class ItemCounter : MonoBehaviour
{
    public string GetItemCountText(int count)
    {
        // 한국어는 단/복수 구분 없음
        if (LocalizationManager.Instance.CurrentLanguage == LanguageType.Korean)
        {
            return LocalizationManager.Instance.GetText("UI_ITEM_COUNT", count);
        }

        // 영어는 단/복수 구분
        if (count == 1)
        {
            return LocalizationManager.Instance.GetText("UI_ITEM_SINGLE");
        }
        else
        {
            return LocalizationManager.Instance.GetText("UI_ITEM_PLURAL", count);
        }
    }
}
```

---

## 주의사항

### ⚠️ CSV 파일 수정 후 재생성 필수

CSV 파일을 수정한 후에는 CSVParser로 LocalizationData 클래스를 재생성해야 합니다.

1. CSV 파일 수정 (`Assets/Data/CSV/LocalizationData.csv`)
2. Unity 에디터 상단 메뉴: `Tools > CSV Parser > Generate All`
3. LocalizationData.cs 자동 재생성 완료

### ⚠️ 키 중복 금지

```csv
# ❌ 나쁜 예: 중복 키
Key,Korean,English
UI_BTN_START,시작,Start
UI_BTN_START,시작하기,Begin

# ✅ 좋은 예: 고유 키
Key,Korean,English
UI_BTN_START,시작,Start
UI_BTN_BEGIN,시작하기,Begin
```

### ⚠️ 키 네이밍 규칙

```csv
# ✅ 좋은 예: 명확한 네이밍
UI_BTN_START         # UI 버튼 - 시작
UI_SCORE             # UI - 점수
ITEM_POTION_NAME     # 아이템 - 물약 이름
DIALOG_NPC_GREETING  # 대화 - NPC 인사

# ❌ 나쁜 예: 모호한 네이밍
START
TEXT1
BTN
```

**권장 네이밍 패턴:**
- `UI_`: UI 요소
- `ITEM_`: 아이템 관련
- `DIALOG_`: 대화 시스템
- `GAME_`: 게임플레이 메시지
- `ERROR_`: 오류 메시지

### ⚠️ 포맷 플레이스홀더 순서

```csv
# ✅ 올바른 예
Key,Korean,English
UI_INFO,레벨 {0} - 점수 {1},Level {0} - Score {1}

# ❌ 잘못된 예 (순서 불일치)
Key,Korean,English
UI_INFO,점수 {1} - 레벨 {0},Level {0} - Score {1}
```

포맷 플레이스홀더 `{0}`, `{1}` 순서는 모든 언어에서 동일해야 합니다!

### ⚠️ 빈 번역 확인

```csv
# ❌ 나쁜 예: 영어 번역 누락
Key,Korean,English
UI_BTN_START,시작,

# ✅ 좋은 예: 모든 번역 작성
Key,Korean,English
UI_BTN_START,시작,Start
```

번역이 비어있으면 `[UI_BTN_START]` 형태로 표시됩니다.

### ⚠️ LocalizedText 컴포넌트 사용 권장

```csharp
// ❌ 비효율적: 수동 갱신
public class ManualText : MonoBehaviour
{
    public TMP_Text text;

    private void Start()
    {
        UpdateText();
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(LanguageType lang)
    {
        UpdateText();
    }

    private void UpdateText()
    {
        text.text = LocalizationManager.Instance.GetText("UI_BTN_START");
    }
}

// ✅ 효율적: LocalizedText 컴포넌트 사용
// TMP_Text에 LocalizedText 컴포넌트 추가
// Key만 설정하면 자동 갱신!
```

### ⚠️ 에디터 전용 기능

`GetTextInEditor()`는 에디터 전용입니다. 빌드에서는 사용할 수 없습니다.

```csharp
#if UNITY_EDITOR
// 에디터에서만 동작
string text = LocalizationManager.Instance.GetTextInEditor("UI_BTN_START");
#endif

// ✅ 런타임에서는 GetText() 사용
string text = LocalizationManager.Instance.GetText("UI_BTN_START");
```

---

## FAQ

### Q1. 언어를 추가하려면?

A. 3단계로 추가할 수 있습니다.

**1단계: LanguageType enum에 추가**
```csharp
public enum LanguageType
{
    Korean,
    English,
    Japanese,  // 추가!
}
```

**2단계: CSV에 컬럼 추가**
```csv
Key,Korean,English,Japanese
UI_BTN_START,시작,Start,スタート
```

**3단계: LocalizationData 재생성**
- Unity 에디터: `Tools > CSV Parser > Generate All`

### Q2. SetFormattedText는 어떻게 사용하나요?

A. LocalizedText 컴포넌트의 메서드입니다.

```csharp
// CSV에 포맷 문자열 등록
// UI_SCORE,점수: {0},Score: {0}

// LocalizedText 컴포넌트 참조
[SerializeField] private LocalizedText scoreText;

// 런타임에서 호출
scoreText.SetFormattedText(1000);
// → "점수: 1000" 또는 "Score: 1000"
```

**주의:** SetFormattedText는 런타임 전용입니다.

### Q3. 언어 변경 시 포맷팅된 텍스트가 유지되지 않습니다.

A. 언어 변경 이벤트를 구독하여 다시 포맷팅해야 합니다.

```csharp
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private LocalizedText scoreText;
    private int currentScore;

    private void Start()
    {
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationManager.IsAlive())
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    public void UpdateScore(int score)
    {
        currentScore = score;
        scoreText.SetFormattedText(score);
    }

    private void OnLanguageChanged(LanguageType newLanguage)
    {
        // 언어 변경 시 현재 점수로 다시 포맷팅
        scoreText.SetFormattedText(currentScore);
    }
}
```

### Q4. 에디터에서 텍스트가 표시되지 않습니다.

A. 다음을 확인하세요:

1. CSV 파일이 `Assets/Data/CSV/LocalizationData.csv` 경로에 있는지
2. Key가 CSV에 존재하는지
3. 해당 언어 컬럼에 번역이 작성되어 있는지

### Q5. 런타임에서 키를 찾을 수 없다고 나옵니다.

A. LocalizationManager가 초기화되지 않았을 수 있습니다.

```csharp
// GameBootstrap이나 초기 씬에서 비동기 초기화
await CSVManager.Instance.Initialize(cancellationToken); // 먼저 CSV 로드
await LocalizationManager.Instance.InitializeLocalizeCSVAsync(cancellationToken); // 이후 Localization 초기화
```

### Q6. 특수문자를 사용할 수 있나요?

A. 네, UTF-8 인코딩을 지원합니다.

```csv
Key,Korean,English
UI_GREETING,안녕하세요! 😊,Hello! 😊
UI_PRICE,가격: ₩1000,Price: $10
```

CSV 파일을 UTF-8 인코딩으로 저장하세요.

### Q7. 여러 줄 텍스트는 어떻게 작성하나요?

A. CSV에서 큰따옴표로 묶으면 됩니다.

```csv
Key,Korean,English
DIALOG_LONG,"안녕하세요.
여러 줄 텍스트입니다.","Hello.
This is multi-line text."
```

TMP_Text에서 자동으로 줄바꿈 처리됩니다.

---

## 요약

**Localization 시스템 사용 3단계:**

1. **CSV에 번역 등록**
2. **LocalizedText 컴포넌트 사용** (UI) 또는 **GetText() 호출** (코드)
3. **언어 변경은 SetLanguage()**

```csharp
// 1. CSV 작성
// Key,Korean,English
// UI_BTN_START,시작,Start

// 2-A. LocalizedText 컴포넌트 사용 (UI)
// TMP_Text에 LocalizedText 추가 → Key 입력

// 2-B. 코드에서 직접 조회
string text = LocalizationManager.Instance.GetText("UI_BTN_START");

// 3. 언어 변경
LocalizationManager.Instance.SetLanguage(LanguageType.English);
```

**포맷팅 사용:**
```csharp
// CSV: UI_SCORE,점수: {0},Score: {0}

// LocalizedText 컴포넌트
[SerializeField] private LocalizedText scoreText;
scoreText.SetFormattedText(1000);

// 또는 직접 조회
string text = LocalizationManager.Instance.GetText("UI_SCORE", 1000);
```

**핵심 원칙:**
- CSV 기반 간편 관리
- LocalizedText 컴포넌트 적극 활용
- 언어 변경 시 자동 갱신
- 포맷팅으로 동적 값 삽입
- 에디터 실시간 미리보기

**추가 정보:**
- 소스 코드: `Assets/Scripts/Common/Localization/LocalizationManager.cs`
- UI 컴포넌트: `Assets/Scripts/Common/Localization/LocalizedText.cs`
- 폰트 설정: `Assets/Scripts/Common/Localization/LocalizationSettings.cs`
- 언어 타입: `Assets/Scripts/Common/Localization/LanguageType.cs`
- CSV 파일: `Assets/Data/CSV/LocalizationData.csv`
- 설정 파일: `Assets/Data/Settings/LocalizationSettings.asset`
