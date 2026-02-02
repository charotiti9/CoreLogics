# 씬 관리 시스템 사용 가이드

## 개요

씬 관리 시스템은 Addressable 기반의 비동기 씬 로드를 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ Addressable 기반 씬 로드 (메모리 관리 용이)
- ✅ 페이드 인/아웃 효과 지원
- ✅ 로딩 UI 자동 표시
- ✅ 백그라운드 프리로드 지원
- ✅ 빌더 패턴으로 다양한 전환 옵션 제공
- ✅ UIManager와 통합
- ✅ 진행률 콜백 지원

---

## 핵심 개념

### 1. SceneLoader

씬 로드를 담당하는 싱글톤 매니저입니다.

```csharp
// 기본 사용법
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
```

### 2. SceneTransitionOptions

씬 전환 방식을 설정하는 옵션 클래스입니다. 빌더 패턴을 지원합니다.

```csharp
public class SceneTransitionOptions
{
    // 씬 로드 완료 후 전환 여부 (백그라운드 프리로드)
    public bool WaitForLoadComplete { get; set; } = false;

    // 로딩 UI 표시 여부
    public bool ShowLoadingUI { get; set; } = false;

    // 페이드 효과 사용 여부
    public bool UseFade { get; set; } = false;

    // 페이드 색상
    public Color FadeColor { get; set; } = Color.black;

    // 페이드 지속 시간 (초)
    public float FadeDuration { get; set; } = 0.5f;

    // 씬 로드 진행률 콜백 (0.0 ~ 1.0)
    public Action<float> OnProgress { get; set; } = null;
}
```

### 3. 전환 모드

#### Immediate 모드 (기본)
즉시 씬 전환을 시작합니다. 현재 씬이 먼저 언로드됩니다.

```
페이드 아웃 → 현재 씬 언로드 → 새 씬 로드 → 페이드 인
```

#### Preload 모드
새 씬을 백그라운드에서 완전히 로드한 후 전환합니다. 현재 씬이 유지됩니다.

```
새 씬 백그라운드 로드 → 페이드 아웃 → 현재 씬 언로드 → 새 씬 활성화 → 페이드 인
```

### 4. UI 컴포넌트

#### LoadingUI
씬 전환 시 표시되는 로딩 화면입니다.

```csharp
[UIAttribute(
    address: "Common/LoadingUI",
    layer: UILayer.Transition,
    useDim: false,
    destroyOnSceneChange: false)]
public class LoadingUI : UIBase
```

#### SceneFadeUI
화면 전체를 덮어 페이드 효과를 제공합니다.

```csharp
[UIAttribute(
    address: "Common/SceneFadeUI",
    layer: UILayer.Transition,
    useDim: false,
    destroyOnSceneChange: false)]
public class SceneFadeUI : UIBase
```

---

## 기본 사용법

### 1. 기본 씬 전환 (효과 없음)

```csharp
using Common.SceneLoader;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GameStarter
{
    public async UniTask StartGameAsync(CancellationToken ct)
    {
        // 기본 전환 (효과 없이 즉시 전환)
        await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
    }
}
```

### 2. 페이드 효과와 함께 전환

```csharp
public async UniTask LoadWithFadeAsync(CancellationToken ct)
{
    var options = SceneTransitionOptions.WithFade(Color.black, 0.5f);
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 3. 로딩 UI와 함께 전환

```csharp
public async UniTask LoadWithLoadingUIAsync(CancellationToken ct)
{
    var options = SceneTransitionOptions.WithLoading();
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 4. 로딩 UI + 페이드 효과

```csharp
public async UniTask LoadWithAllEffectsAsync(CancellationToken ct)
{
    var options = SceneTransitionOptions.WithLoadingAndFade(Color.black, 0.5f);
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 5. 백그라운드 프리로드 후 전환

현재 씬을 유지하면서 새 씬을 백그라운드에서 로드합니다.

```csharp
public async UniTask LoadWithPreloadAsync(CancellationToken ct)
{
    // 프리로드만
    var options = SceneTransitionOptions.Preloaded();
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}

public async UniTask LoadWithPreloadAndFadeAsync(CancellationToken ct)
{
    // 프리로드 + 페이드
    var options = SceneTransitionOptions.PreloadedWithFade(Color.black, 0.5f);
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}

public async UniTask LoadWithFullOptionsAsync(CancellationToken ct)
{
    // 프리로드 + 로딩 UI + 페이드 (풀 옵션)
    var options = SceneTransitionOptions.PreloadedWithLoadingAndFade(Color.black, 0.5f);
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 6. 커스텀 옵션

```csharp
public async UniTask LoadWithCustomOptionsAsync(CancellationToken ct)
{
    var options = new SceneTransitionOptions
    {
        WaitForLoadComplete = true,
        ShowLoadingUI = true,
        UseFade = true,
        FadeColor = new Color(0.2f, 0.2f, 0.2f),  // 어두운 회색
        FadeDuration = 0.3f
    };

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 7. 진행률 콜백 사용

```csharp
public async UniTask LoadWithProgressAsync(CancellationToken ct)
{
    var options = new SceneTransitionOptions
    {
        ShowLoadingUI = true,
        OnProgress = (progress) =>
        {
            // progress: 0.0 ~ 1.0
            Debug.Log($"로딩 진행률: {progress * 100:F0}%");
        }
    };

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

---

## 실전 예제

### 타이틀에서 게임 씬으로 전환

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.SceneLoader;
using Cysharp.Threading.Tasks;
using System.Threading;

public class TitleController : MonoBehaviour
{
    public Button startButton;

    private CancellationTokenSource cts;

    private void Start()
    {
        cts = new CancellationTokenSource();
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void OnStartButtonClicked()
    {
        // 버튼 비활성화 (중복 클릭 방지)
        startButton.interactable = false;

        StartGameAsync(cts.Token).Forget();
    }

    private async UniTaskVoid StartGameAsync(CancellationToken ct)
    {
        // 페이드 효과와 함께 게임 씬으로 전환
        var options = SceneTransitionOptions.WithFade(Color.black, 0.5f);
        await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

### 스테이지 클리어 후 다음 스테이지로 전환

```csharp
using Common.SceneLoader;
using Cysharp.Threading.Tasks;
using System.Threading;

public class StageManager
{
    private int currentStage;

    public async UniTask LoadNextStageAsync(CancellationToken ct)
    {
        currentStage++;

        // 로딩 UI와 페이드 효과 사용
        var options = SceneTransitionOptions.WithLoadingAndFade(Color.black, 0.3f);
        await SceneLoader.Instance.LoadSceneAsync($"Scenes/Stage{currentStage}", options, ct);
    }

    public async UniTask ReturnToLobbyAsync(CancellationToken ct)
    {
        // 프리로드로 부드러운 전환
        var options = SceneTransitionOptions.PreloadedWithFade(Color.black, 0.5f);
        await SceneLoader.Instance.LoadSceneAsync("Scenes/Lobby", options, ct);
    }
}
```

### 무거운 씬 프리로드

```csharp
using Common.SceneLoader;
using Cysharp.Threading.Tasks;
using System.Threading;

public class HeavySceneLoader
{
    /// <summary>
    /// 무거운 씬을 백그라운드에서 로드하면서 로딩 UI를 표시합니다.
    /// 현재 씬에서 플레이어가 계속 활동할 수 있습니다.
    /// </summary>
    public async UniTask LoadHeavySceneAsync(CancellationToken ct)
    {
        // 프리로드 + 로딩 UI
        // 씬이 완전히 로드될 때까지 현재 씬 유지
        var options = SceneTransitionOptions.PreloadedWithLoading();
        await SceneLoader.Instance.LoadSceneAsync("Scenes/OpenWorld", options, ct);
    }
}
```

### 로딩 중 확인

```csharp
using Common.SceneLoader;

public class LoadingChecker
{
    public void CheckLoading()
    {
        if (SceneLoader.Instance.IsLoading)
        {
            // 로딩 중에는 특정 동작 제한
            Debug.Log("씬 로딩 중입니다...");
        }
    }
}
```

### 진행률 표시 (커스텀 LoadingUI)

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.SceneLoader;
using Common.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

[UIAttribute(
    address: "UI/ProgressLoadingUI",
    layer: UILayer.Transition,
    useDim: false,
    destroyOnSceneChange: false)]
public class ProgressLoadingUI : LoadingUI
{
    [SerializeField]
    private Slider progressBar;

    [SerializeField]
    private Text percentText;

    public override void OnSpawn()
    {
        progressBar.value = 0f;
        percentText.text = "0%";
    }

    /// <summary>
    /// SceneLoader가 자동으로 호출합니다.
    /// </summary>
    public override void UpdateProgress(float progress)
    {
        progressBar.value = progress;
        percentText.text = $"{progress * 100:F0}%";
    }
}

// 사용 예시
public class GameLoader
{
    public async UniTask LoadGameAsync(CancellationToken ct)
    {
        // ShowLoadingUI = true면 LoadingUI.UpdateProgress가 자동 호출됨
        var options = new SceneTransitionOptions
        {
            ShowLoadingUI = true,
            WaitForLoadComplete = true
        };

        await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
    }
}
```

### 외부에서 진행률 받아 처리하기

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.SceneLoader;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ExternalProgressHandler : MonoBehaviour
{
    public Slider externalProgressBar;

    public async UniTask LoadWithExternalProgressAsync(CancellationToken ct)
    {
        var options = new SceneTransitionOptions
        {
            WaitForLoadComplete = true,
            OnProgress = UpdateExternalProgress
        };

        await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
    }

    private void UpdateExternalProgress(float progress)
    {
        // LoadingUI와 별개로 외부 UI에서 진행률 표시
        externalProgressBar.value = progress;
    }
}
```

---

## SceneTransitionOptions 프리셋

| 프리셋 메서드 | WaitForLoadComplete | ShowLoadingUI | UseFade | 설명 |
|--------------|---------------------|---------------|---------|------|
| `Default` | false | false | false | 효과 없이 즉시 전환 |
| `WithLoading()` | false | true | false | 로딩 UI만 표시 |
| `WithFade()` | false | false | true | 페이드 효과만 |
| `WithLoadingAndFade()` | false | true | true | 로딩 UI + 페이드 |
| `Preloaded()` | true | false | false | 백그라운드 프리로드 |
| `PreloadedWithLoading()` | true | true | false | 프리로드 + 로딩 UI |
| `PreloadedWithFade()` | true | false | true | 프리로드 + 페이드 |
| `PreloadedWithLoadingAndFade()` | true | true | true | 풀 옵션 |

---

## 주의사항

### ⚠️ Addressable 씬 등록 필수

씬은 반드시 Addressables에 등록되어 있어야 합니다.

```csharp
// ❌ 나쁜 예: 등록되지 않은 씬
await SceneLoader.Instance.LoadSceneAsync("UnregisteredScene", ct);
// 오류 발생!

// ✅ 좋은 예: Addressables에 등록된 씬
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
```

### ⚠️ UIManager 초기화 필요

페이드/로딩 UI를 사용하려면 UIManager가 초기화되어 있어야 합니다.

```csharp
// UIManager가 초기화되지 않으면 경고 로그 출력 후 UI 없이 진행
[SceneLoader] UIManager가 초기화되지 않아 페이드 UI를 표시할 수 없습니다.
```

### ⚠️ 중복 로드 방지

이미 씬 로드가 진행 중이면 새 요청은 무시됩니다.

```csharp
// ❌ 나쁜 예: 중복 호출
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);  // 무시됨

// ✅ 좋은 예: IsLoading 체크
if (!SceneLoader.Instance.IsLoading)
{
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
}
```

### ⚠️ CancellationToken 전달 필수

씬 로드 중 취소가 필요할 수 있으므로 항상 CancellationToken을 전달합니다.

```csharp
// ❌ 나쁜 예: CancellationToken 없음
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", CancellationToken.None);

// ✅ 좋은 예: CancellationToken 전달
var cts = new CancellationTokenSource();
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", cts.Token);
```

### ⚠️ UI 프리팹 설정

LoadingUI와 SceneFadeUI 프리팹이 Addressables에 등록되어 있어야 합니다.

- LoadingUI: `Common/LoadingUI`
- SceneFadeUI: `Common/SceneFadeUI`

---

## FAQ

### Q1. 페이드 색상을 씬마다 다르게 하고 싶습니다.

A. `SceneTransitionOptions`를 매번 새로 생성하여 다른 색상을 지정합니다.

```csharp
// 검은색 페이드
var blackFade = SceneTransitionOptions.WithFade(Color.black, 0.5f);

// 흰색 페이드
var whiteFade = SceneTransitionOptions.WithFade(Color.white, 0.5f);

// 커스텀 색상
var customFade = SceneTransitionOptions.WithFade(new Color(0.1f, 0.1f, 0.2f), 0.3f);
```

### Q2. 로딩 진행률을 표시하고 싶습니다.

A. 두 가지 방법이 있습니다.

**방법 1: LoadingUI 커스터마이징 (권장)**

`LoadingUI`를 상속받아 `UpdateProgress()`를 오버라이드하세요. `ShowLoadingUI = true`면 SceneLoader가 자동으로 호출합니다.

```csharp
public class MyLoadingUI : LoadingUI
{
    public Slider progressBar;

    public override void UpdateProgress(float progress)
    {
        progressBar.value = progress;
    }
}
```

**방법 2: OnProgress 콜백 사용**

`SceneTransitionOptions.OnProgress`에 콜백을 등록하세요.

```csharp
var options = new SceneTransitionOptions
{
    OnProgress = (progress) => Debug.Log($"{progress * 100:F0}%")
};
```

### Q3. 특정 씬에서만 프리로드를 사용하고 싶습니다.

A. 씬별로 다른 옵션을 사용하면 됩니다.

```csharp
public async UniTask LoadSceneByTypeAsync(string sceneName, CancellationToken ct)
{
    SceneTransitionOptions options;

    // 무거운 씬은 프리로드 사용
    if (sceneName.Contains("OpenWorld") || sceneName.Contains("Boss"))
    {
        options = SceneTransitionOptions.PreloadedWithLoadingAndFade(Color.black, 0.5f);
    }
    // 가벼운 씬은 즉시 전환
    else
    {
        options = SceneTransitionOptions.WithFade(Color.black, 0.3f);
    }

    await SceneLoader.Instance.LoadSceneAsync(sceneName, options, ct);
}
```

### Q4. 씬 전환이 취소되면 어떻게 되나요?

A. `OperationCanceledException`이 발생하고 로그가 출력됩니다. 현재 씬 상태가 유지됩니다.

```csharp
try
{
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
}
catch (OperationCanceledException)
{
    // 씬 로드가 취소됨
    Debug.Log("씬 전환이 취소되었습니다.");
}
```

### Q5. LoadingUI/SceneFadeUI를 커스터마이징하고 싶습니다.

A. 해당 클래스를 상속받아 커스텀 UI를 만들 수 있습니다. 또는 프리팹을 수정하여 디자인을 변경하세요.

```csharp
// 커스텀 로딩 UI 예시
[UIAttribute(
    address: "UI/CustomLoadingUI",
    layer: UILayer.Transition,
    useDim: false,
    destroyOnSceneChange: false)]
public class CustomLoadingUI : LoadingUI
{
    public Slider progressBar;
    public Text percentText;
    public Text tipText;

    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        await base.OnShowAsync(ct);

        // 진행률 초기화
        progressBar.value = 0f;
        percentText.text = "0%";

        // 랜덤 팁 표시
        tipText.text = GetRandomTip();
    }

    // SceneLoader가 자동으로 호출
    public override void UpdateProgress(float progress)
    {
        progressBar.value = progress;
        percentText.text = $"{progress * 100:F0}%";
    }

    private string GetRandomTip()
    {
        string[] tips = { "팁 1", "팁 2", "팁 3" };
        return tips[UnityEngine.Random.Range(0, tips.Length)];
    }
}
```

### Q6. 페이드 인/아웃 시간을 다르게 설정하고 싶습니다.

A. 현재는 동일한 `FadeDuration`을 사용합니다. 다른 시간이 필요하면 SceneLoader를 확장하거나 SceneFadeUI를 직접 제어하세요.

---

## 요약

**씬 관리 시스템 사용 3단계:**

1. **SceneTransitionOptions 선택/생성**
2. **LoadSceneAsync 호출**
3. **완료 대기**

```csharp
// 1. 기본 전환
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);

// 2. 옵션과 함께 전환
var options = SceneTransitionOptions.WithFade(Color.black, 0.5f);
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
```

**프리셋 빠른 참조:**

```csharp
// 효과 없이 즉시 전환
SceneTransitionOptions.Default

// 페이드만
SceneTransitionOptions.WithFade(Color.black, 0.5f)

// 로딩 UI만
SceneTransitionOptions.WithLoading()

// 로딩 UI + 페이드
SceneTransitionOptions.WithLoadingAndFade(Color.black, 0.5f)

// 프리로드 + 페이드 (권장)
SceneTransitionOptions.PreloadedWithFade(Color.black, 0.5f)

// 풀 옵션
SceneTransitionOptions.PreloadedWithLoadingAndFade(Color.black, 0.5f)
```

**핵심 원칙:**
- Addressable 기반 씬 로드
- 항상 CancellationToken 전달
- 중복 로드 자동 방지
- UIManager와 통합 (페이드/로딩 UI)
- 진행률 콜백: `OnProgress` 또는 `LoadingUI.UpdateProgress()` 활용

**추가 정보:**
- 소스 코드: `Assets/Scripts/Common/SceneLoader/SceneLoader.cs`
- 옵션 클래스: `Assets/Scripts/Common/SceneLoader/SceneTransitionOptions.cs`
- 로딩 UI: `Assets/Scripts/Common/SceneLoader/UI/LoadingUI.cs`
- 페이드 UI: `Assets/Scripts/Common/SceneLoader/UI/SceneFadeUI.cs`
