# UI 시스템 사용 가이드

## 개요

UI 시스템은 게임의 모든 UI를 `UIManager`에서 중앙 관리하는 시스템입니다.  
현재 구현은 `PoolAddress`가 아니라 `UIAttribute`를 기준으로 UI 정보를 읽고, `Spawn / Show / Hide / Destroy` 생명주기를 분리해서 관리합니다.

**장점:**
- ✅ Addressable 기반 UI 로드
- ✅ 레이어 시스템으로 UI 계층 구조화
- ✅ Dim 자동 처리
- ✅ PopUp 스택 관리 지원
- ✅ UI 생명주기 명확한 분리
- ✅ 씬 전환 유지 여부를 UI 단위로 제어 가능

---

## 핵심 개념

### 1. UIAttribute

모든 UI 클래스에는 반드시 `UIAttribute`를 선언해야 합니다.

```csharp
[UIAttribute(
    address: "UI/MainMenu",
    layer: UILayer.Overlay,
    useDim: false,
    destroyOnSceneChange: true)]
public class MainMenuUI : UIBase
{
}
```

**각 옵션의 의미:**
- `address`: Addressable 주소
- `layer`: UI가 배치될 레이어
- `useDim`: Show 시 Dim 자동 표시 여부
- `destroyOnSceneChange`: 씬 전환 시 자동 제거 여부

### 2. UI 레이어

현재 구현의 UI 레이어는 다음과 같습니다.

```csharp
public enum UILayer
{
    Background = 0,
    HUD = 1,
    Overlay = 2,
    PopUp = 3,
    System = 4,
    Transition = 5
}
```

**사용 예시:**
- **HUD**: 체력바, 미니맵, 플레이 정보
- **Overlay**: 메뉴, 인벤토리, 상점
- **PopUp**: 확인창, 보상창, 경고창
- **System**: 토스트, 시스템 알림
- **Transition**: 로딩 UI, 페이드 UI

### 3. UI 생명주기

현재 구현 기준 UI 생명주기는 다음 순서로 동작합니다.

```text
1. SpawnAsync<T>()
   ↓
2. OnSpawn()                // 최초 생성 시 1회
   ↓
3. ShowAsync<T>()
   ↓
4. OnShowAsync(ct)          // 표시될 때마다
   ↓
5. ShowAnimation            // 있으면 재생
   ↓
6. Hide<T>()
   ↓
7. OnHideAsync(ct)          // 숨길 때마다
   ↓
8. HideAnimation            // immediate=false 이고 있으면 재생
   ↓
9. Destroy<T>()
   ↓
10. OnBeforeDestroy()       // 최종 제거 직전
```

### 4. Spawn과 Show의 차이

현재 `UIManager`는 `ShowAsync<T>()` 호출 시 UI를 자동 생성하지 않습니다.

- `SpawnAsync<T>()`: UI 인스턴스를 생성하고 메모리에 유지
- `ShowAsync<T>()`: 이미 생성된 UI를 표시
- `Hide<T>()`: 표시만 숨기고 인스턴스는 유지
- `Destroy<T>()`: UI를 완전히 제거

즉, 먼저 `SpawnAsync<T>()`를 호출한 뒤 `ShowAsync<T>()`를 호출해야 합니다.

### 5. PopUp 스택

`UILayer.PopUp`으로 표시된 UI는 자동으로 스택에 등록됩니다.  
뒤로가기 입력 시 `UIManager.HandleBackKey()`가 스택 최상단 팝업을 닫습니다.

---

## 기본 사용법

### 1. UI 클래스 작성

`UIBase`를 상속하고 `UIAttribute`를 선언합니다.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Common.UI;
using UnityEngine;
using UnityEngine.UI;

[UIAttribute(
    address: "UI/MainMenu",
    layer: UILayer.Overlay,
    useDim: false,
    destroyOnSceneChange: true)]
public class MainMenuUI : UIBase
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionButton;

    /// <summary>
    /// UI가 생성될 때 1회만 초기화합니다.
    /// </summary>
    public override void OnSpawn()
    {
        startButton.onClick.AddListener(OnClickStart);
        optionButton.onClick.AddListener(OnClickOption);
    }

    /// <summary>
    /// UI가 표시될 때마다 최신 상태를 반영합니다.
    /// </summary>
    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// UI가 숨겨질 때 표시 상태를 정리합니다.
    /// </summary>
    public override async UniTask OnHideAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// UI가 완전히 제거되기 전에 이벤트를 정리합니다.
    /// </summary>
    public override void OnBeforeDestroy()
    {
        startButton.onClick.RemoveListener(OnClickStart);
        optionButton.onClick.RemoveListener(OnClickOption);
    }

    private void OnClickStart()
    {
    }

    private void OnClickOption()
    {
    }
}
```

### 2. UI 생성 후 표시

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Common.UI;

public class LobbyPresenter
{
    public async UniTask ShowMainMenuAsync(CancellationToken ct)
    {
        if (!UIManager.Instance.IsSpawned<MainMenuUI>())
        {
            await UIManager.Instance.SpawnAsync<MainMenuUI>(ct);
        }

        await UIManager.Instance.ShowAsync<MainMenuUI>(ct: ct);
    }
}
```

### 3. 데이터가 있는 UI 표시

데이터 전달이 필요한 경우 `UIBase<TData>`를 사용합니다.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Common.UI;
using TMPro;
using UnityEngine;

public class RewardPopupData
{
    public string Title;
    public int GoldAmount;
}

[UIAttribute(
    address: "UI/RewardPopup",
    layer: UILayer.PopUp,
    useDim: true,
    destroyOnSceneChange: true)]
public class RewardPopupUI : UIBase<RewardPopupData>
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldText;

    protected override void OnDataChanged(RewardPopupData data)
    {
        titleText.text = data.Title;
        goldText.text = data.GoldAmount.ToString();
    }

    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }
}
```

```csharp
public class RewardService
{
    public async UniTask OpenRewardPopupAsync(CancellationToken ct)
    {
        if (!UIManager.Instance.IsSpawned<RewardPopupUI>())
        {
            await UIManager.Instance.SpawnAsync<RewardPopupUI>(ct);
        }

        var data = new RewardPopupData
        {
            Title = "클리어 보상",
            GoldAmount = 300
        };

        await UIManager.Instance.ShowAsync<RewardPopupUI, RewardPopupData>(data, ct);
    }
}
```

### 4. UI 숨기기

```csharp
UIManager.Instance.Hide<RewardPopupUI>();
UIManager.Instance.Hide<RewardPopupUI>(immediate: true);
```

### 5. 특정 레이어 전체 숨기기

```csharp
UIManager.Instance.HideAll(UILayer.PopUp);
UIManager.Instance.HideAll(UILayer.System, immediate: true);
```

### 6. UI 완전 제거

```csharp
UIManager.Instance.Destroy<MainMenuUI>();
```

---

## 조회 API

생성/표시 상태는 다음 API로 확인할 수 있습니다.

```csharp
bool isSpawned = UIManager.Instance.IsSpawned<MainMenuUI>();
bool isShowing = UIManager.Instance.IsShowing<MainMenuUI>();

MainMenuUI spawnedUI = UIManager.Instance.GetSpawned<MainMenuUI>();
MainMenuUI showingUI = UIManager.Instance.GetShowing<MainMenuUI>();
```

**의미:**
- `IsSpawned<T>()`: 메모리에 생성되었는지 확인
- `IsShowing<T>()`: 현재 표시 중인지 확인
- `GetSpawned<T>()`: 생성된 인스턴스 반환
- `GetShowing<T>()`: 현재 표시 중인 인스턴스 반환

---

## 예제 모음

### 자주 쓰는 UI를 재사용하는 패턴

```csharp
public async UniTask OpenInventoryAsync(CancellationToken ct)
{
    if (!UIManager.Instance.IsSpawned<InventoryUI>())
    {
        await UIManager.Instance.SpawnAsync<InventoryUI>(ct);
    }

    if (!UIManager.Instance.IsShowing<InventoryUI>())
    {
        await UIManager.Instance.ShowAsync<InventoryUI>(ct: ct);
    }
}
```

### 뒤로가기 처리

```csharp
private void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        UIManager.Instance.HandleBackKey();
    }
}
```

### 씬 전환 후에도 유지되는 UI

```csharp
[UIAttribute(
    address: "UI/GameHUD",
    layer: UILayer.HUD,
    useDim: false,
    destroyOnSceneChange: false)]
public class GameHUDUI : UIBase
{
}
```

---

## 주의사항

### 1. `UIAttribute`는 필수입니다

현재 구현은 `UIAttribute`를 기반으로 주소, 레이어, Dim 여부를 읽습니다.  
Attribute가 없으면 `UIManager`와 `UIBase`에서 에러 로그가 발생합니다.

### 2. `ShowAsync<T>()` 전에 `SpawnAsync<T>()`를 호출해야 합니다

예전 방식처럼 `ShowAsync<T>()`만 호출해서 자동 생성되지 않습니다.

```csharp
await UIManager.Instance.SpawnAsync<MyUI>(ct);
await UIManager.Instance.ShowAsync<MyUI>(ct: ct);
```

### 3. 레이어는 코드 오버라이드가 아니라 Attribute 기준입니다

예전 방식처럼 `public override UILayer Layer => ...`를 구현하지 않습니다.  
현재 레이어 정보는 `UIAttribute`에서 읽어옵니다.

### 4. Dim은 `useDim`으로 제어합니다

팝업 배경을 어둡게 하고 싶다면 `UIAttribute(useDim: true)`로 설정합니다.

### 5. Addressable 주소가 정확해야 합니다

```csharp
[UIAttribute(address: "UI/MainMenu", layer: UILayer.Overlay)]
public class MainMenuUI : UIBase
{
}
```

이 경우 Addressables에 등록된 주소도 반드시 `"UI/MainMenu"`여야 합니다.

---

## 빠른 체크리스트

- `UIAttribute`를 선언했는가?
- Addressable 주소가 실제 등록값과 일치하는가?
- `ShowAsync<T>()` 전에 `SpawnAsync<T>()`를 호출했는가?
- 1회 초기화는 `OnSpawn()`에 작성했는가?
- 표시 시 갱신은 `OnShowAsync()` 또는 `OnDataChanged()`에 작성했는가?
- 씬 전환 유지 여부를 `destroyOnSceneChange`로 명시했는가?

---

## 관련 파일

- `Assets/Scripts/Common/UI/Core/UIManager.cs`
- `Assets/Scripts/Common/UI/Core/UIBase.cs`
- `Assets/Scripts/Common/UI/Core/UIAddressAttribute.cs`
- `Assets/Scripts/Common/UI/Core/UILayer.cs`
