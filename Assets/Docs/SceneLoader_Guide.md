# 씬 관리 시스템 사용 가이드

## 개요

씬 관리 시스템은 Addressable 기반의 비동기 씬 로드를 중앙에서 관리하는 시스템입니다.  
현재 구현은 `SceneLoader`, `SceneTransitionOptions`, `LoadingUI`, `SceneFadeUI`를 조합해 즉시 전환과 프리로드 전환을 모두 지원합니다.

**장점:**
- ✅ Addressable 기반 씬 로드
- ✅ 즉시 전환 / 프리로드 전환 지원
- ✅ 로딩 UI 자동 표시
- ✅ 페이드 인/아웃 효과 지원
- ✅ 진행률 콜백 지원
- ✅ 씬 활성화 직후 준비 작업 훅 지원

---

## 핵심 개념

### 1. SceneLoader

씬 로드를 담당하는 싱글톤 매니저입니다.

```csharp
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
```

현재 구현은 다음 진입 API를 제공합니다.

```csharp
await SceneLoader.Instance.LoadSceneAsync(sceneAddress, ct);
await SceneLoader.Instance.LoadSceneAsync(sceneAddress, options, ct);

await SceneLoader.Instance.TransitionToSceneAsync(sceneAddress, onSceneReady, ct);
await SceneLoader.Instance.TransitionToSceneAsync(sceneAddress, options, onSceneReady, ct);
```

### 2. SceneTransitionOptions

씬 전환 방식을 설정하는 옵션 클래스입니다. 빌더 패턴을 지원합니다.

```csharp
public class SceneTransitionOptions
{
    public bool WaitForLoadComplete { get; set; } = false;
    public bool ShowLoadingUI { get; set; } = false;
    public bool UseFade { get; set; } = false;
    public Color FadeColor { get; set; } = Color.black;
    public float FadeDuration { get; set; } = 0.5f;
    public Action<float> OnProgress { get; set; } = null;
    public Func<CancellationToken, UniTask> OnSceneReady { get; set; } = null;
}
```

**각 옵션의 의미:**
- `WaitForLoadComplete`: 씬을 백그라운드에서 모두 로드한 뒤 전환할지 여부
- `ShowLoadingUI`: 로딩 UI 표시 여부
- `UseFade`: 페이드 효과 사용 여부
- `FadeColor`: 페이드 색상
- `FadeDuration`: 페이드 시간
- `OnProgress`: 로딩 진행률 콜백
- `OnSceneReady`: 씬 활성화 직후 실행할 추가 준비 작업

### 3. 전환 모드

#### Immediate 모드 (기본)

즉시 씬 전환을 시작합니다.

```text
페이드 아웃(옵션)
→ 로딩 UI 표시(옵션)
→ 현재 씬 언로드
→ 새 씬 로드 및 즉시 활성화
→ OnSceneReady 실행
→ 로딩 UI 숨김
→ 페이드 인
```

#### Preload 모드

새 씬을 백그라운드에서 모두 로드한 후 전환합니다.

```text
로딩 UI 표시(옵션)
→ 새 씬 백그라운드 로드
→ 로드 완료 대기
→ 페이드 아웃(옵션)
→ 현재 씬 언로드
→ 새 씬 활성화
→ OnSceneReady 실행
→ 로딩 UI 숨김
→ 페이드 인
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
using System.Threading;
using Cysharp.Threading.Tasks;
using Common.SceneLoader;

public class GameStarter
{
    public async UniTask StartGameAsync(CancellationToken ct)
    {
        await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
    }
}
```

### 2. 페이드 효과와 함께 전환

```csharp
public async UniTask LoadWithFadeAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.WithFade(UnityEngine.Color.black, 0.5f);

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 3. 로딩 UI와 함께 전환

```csharp
public async UniTask LoadWithLoadingUIAsync(CancellationToken ct)
{
    SceneTransitionOptions options = SceneTransitionOptions.WithLoading();
    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 4. 로딩 UI + 페이드 효과

```csharp
public async UniTask LoadWithFullEffectAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.WithLoadingAndFade(UnityEngine.Color.black, 0.5f);

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 5. 프리로드 방식으로 전환

```csharp
public async UniTask LoadPreloadedAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.PreloadedWithLoadingAndFade(UnityEngine.Color.black, 0.5f);

    await SceneLoader.Instance.LoadSceneAsync("Scenes/OpenWorld", options, ct);
}
```

---

## OnSceneReady 사용법

현재 구현에는 씬 활성화 직후 실행되는 `OnSceneReady` 훅이 있습니다.  
씬이 열린 직후 추가 초기화가 필요하면 이 지점을 사용합니다.

### 1. TransitionToSceneAsync 사용

```csharp
public async UniTask MoveToBattleAsync(CancellationToken ct)
{
    await SceneLoader.Instance.TransitionToSceneAsync(
        "Scenes/Battle",
        async readyCt =>
        {
            await UniTask.CompletedTask;
            // 씬 진입 직후 초기화
        },
        ct);
}
```

### 2. options.OnSceneReady 직접 지정

```csharp
public async UniTask MoveToLobbyAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.WithFade(UnityEngine.Color.black, 0.3f);

    options.OnSceneReady = async readyCt =>
    {
        await UniTask.CompletedTask;
        // 씬 활성화 직후 후처리
    };

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Lobby", options, ct);
}
```

---

## 진행률 콜백 사용

로딩 진행률을 직접 받으려면 `OnProgress`를 사용합니다.

```csharp
public async UniTask LoadStageAsync(CancellationToken ct)
{
    SceneTransitionOptions options = SceneTransitionOptions.WithLoading();
    options.OnProgress = progress =>
    {
        UnityEngine.Debug.Log($"로딩 진행률: {progress:P0}");
    };

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Stage01", options, ct);
}
```

`LoadingUI`를 커스텀 구현한 경우 `UpdateProgress(float progress)`를 오버라이드해 직접 표시를 연결할 수 있습니다.

---

## 프리셋 옵션

| 프리셋 메서드 | WaitForLoadComplete | ShowLoadingUI | UseFade | 설명 |
|---|---|---|---|---|
| `Default` | false | false | false | 효과 없는 즉시 전환 |
| `WithLoading()` | false | true | false | 로딩 UI만 표시 |
| `WithFade()` | false | false | true | 페이드 효과만 |
| `WithLoadingAndFade()` | false | true | true | 로딩 UI + 페이드 |
| `Preloaded()` | true | false | false | 프리로드 후 전환 |
| `PreloadedWithLoading()` | true | true | false | 프리로드 + 로딩 UI |
| `PreloadedWithFade()` | true | false | true | 프리로드 + 페이드 |
| `PreloadedWithLoadingAndFade()` | true | true | true | 프리로드 + 풀 옵션 |

---

## 예제 모음

### 타이틀에서 게임 씬 진입

```csharp
public async UniTask StartGameAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.WithLoadingAndFade(UnityEngine.Color.black, 0.3f);

    await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", options, ct);
}
```

### 무거운 씬을 미리 로드한 뒤 전환

```csharp
public async UniTask EnterOpenWorldAsync(CancellationToken ct)
{
    SceneTransitionOptions options =
        SceneTransitionOptions.PreloadedWithLoadingAndFade(UnityEngine.Color.black, 0.5f);

    await SceneLoader.Instance.LoadSceneAsync("Scenes/OpenWorld", options, ct);
}
```

### 씬 진입 직후 UI 초기화

```csharp
public async UniTask MoveToLobbyAsync(CancellationToken ct)
{
    await SceneLoader.Instance.TransitionToSceneAsync(
        "Scenes/Lobby",
        async readyCt =>
        {
            await UniTask.CompletedTask;
            // HUD Spawn, 데이터 연결 등
        },
        ct);
}
```

---

## 주의사항

### 1. 씬 주소는 Addressable 주소 기준입니다

`LoadSceneAsync("Scenes/Game", ct)`의 `"Scenes/Game"`는 Addressables에 등록된 씬 주소여야 합니다.

### 2. 중복 로드는 무시됩니다

`SceneLoader`는 내부적으로 `isLoading`을 사용해 동시 로드를 막습니다.  
이미 로드 중이면 경고 로그를 남기고 추가 요청을 무시합니다.

### 3. CancellationToken 전달을 권장합니다

씬 전환은 비동기 작업이므로 호출부의 `CancellationToken`을 그대로 전달하는 편이 안전합니다.

```csharp
await SceneLoader.Instance.LoadSceneAsync("Scenes/Game", ct);
```

### 4. 전환 UI는 씬 전환 후에도 유지됩니다

`LoadingUI`와 `SceneFadeUI`는 `destroyOnSceneChange: false`로 정의되어 있어 씬 전환 중에도 유지됩니다.

### 5. 씬 활성화 직후 초기화는 `OnSceneReady`에 모으는 편이 좋습니다

전환 흐름을 분산시키지 않고, 씬 활성화 직후 필요한 후처리를 한 곳에서 관리할 수 있습니다.

---

## 빠른 체크리스트

- 씬이 Addressables에 등록되어 있는가?
- 씬 주소 문자열이 실제 등록 주소와 일치하는가?
- Immediate 전환인지 Preload 전환인지 결정했는가?
- 로딩 UI와 페이드가 필요한지 정했는가?
- 씬 활성화 후 초기화가 있으면 `OnSceneReady`를 사용했는가?
- 호출부에서 `CancellationToken`을 전달했는가?

---

## 관련 파일

- `Assets/Scripts/Common/SceneLoader/SceneLoader.cs`
- `Assets/Scripts/Common/SceneLoader/SceneTransitionOptions.cs`
- `Assets/Scripts/Common/SceneLoader/UI/LoadingUI.cs`
- `Assets/Scripts/Common/SceneLoader/UI/SceneFadeUI.cs`
