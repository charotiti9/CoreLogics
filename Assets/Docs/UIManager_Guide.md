# UI 시스템 사용 가이드

## 개요

UI 시스템은 게임의 모든 UI를 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ UI 생명주기 자동 관리
- ✅ 레이어 시스템으로 UI 계층 구조화
- ✅ Pool 시스템과 통합 (UI 재사용)
- ✅ Addressable과 통합 (리소스 관리)
- ✅ Dim(배경 어둡게) 자동 처리
- ✅ 뒤로가기 스택 지원

---

## 핵심 개념

### 1. UI 레이어

UI는 6개의 레이어로 구분되며, 숫자가 클수록 위에 표시됩니다.

```csharp
public enum UILayer
{
    Background = 0,  // 배경 UI
    HUD = 1,         // 게임플레이 HUD (체력바, 미니맵 등)
    Overlay = 2,     // 일반 UI (메인 메뉴, 인벤토리 등)
    PopUp = 3,       // 팝업 (다이얼로그, 보상 창 등)
    System = 4,      // 시스템 UI (토스트, 알림 등)
    Transition = 5   // 전환 UI (로딩 화면, 페이드 등)
}
```

**사용 예시:**
- **HUD**: 게임 중 항상 보이는 UI
- **Overlay**: 메뉴, 인벤토리 등
- **PopUp**: 확인 창, 보상 창 등 일시적 UI
- **System**: 토스트 메시지
- **Transition**: 씬 전환 효과

### 2. UI 생명주기

```
1. OnInitialize (최초 1회만)
   ↓
2. OnShowAsync (표시될 때마다)
   ↓
3. ShowAnimation (선택)
   ↓
4. [UI 표시 중]
   ↓
5. OnHideAsync (숨길 때마다)
   ↓
6. HideAnimation (선택)
   ↓
7. [UI 숨김] → Pool로 반환
```

### 3. Pool 시스템 통합

UI는 자동으로 Pool에서 관리됩니다.
- 첫 Show: Pool에서 가져오거나 새로 생성
- Hide: Pool로 반환 (재사용)
- 메모리 효율 극대화

---

## 기본 사용법

### 1. UI 클래스 작성

`UIBase`를 상속받아 UI 클래스를 작성합니다.

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;

// PoolAddress Attribute 필수!
[PoolAddress("UI/MainMenu")]
public class MainMenuUI : UIBase
{
    // UI 레이어 지정
    public override UILayer Layer => UILayer.Overlay;

    // UI 요소
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;

    // 최초 1회 초기화
    public override void OnInitialize(object data)
    {
        // 버튼 이벤트 등록
        startButton.onClick.AddListener(OnStartClicked);
        optionsButton.onClick.AddListener(OnOptionsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
    }

    // UI가 표시될 때마다 호출
    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        Debug.Log("메인 메뉴 표시");
        await UniTask.CompletedTask;
    }

    // UI가 숨겨질 때마다 호출
    public override async UniTask OnHideAsync(CancellationToken ct)
    {
        Debug.Log("메인 메뉴 숨김");
        await UniTask.CompletedTask;
    }

    private void OnStartClicked()
    {
        Debug.Log("게임 시작");
    }

    private void OnOptionsClicked()
    {
        Debug.Log("옵션 열기");
    }

    private void OnQuitClicked()
    {
        Debug.Log("게임 종료");
    }
}
```

### 2. UI 표시하기

`UIManager.Instance.ShowAsync<T>()`로 UI를 표시합니다.

```csharp
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.UI;

public class GameStarter : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 메인 메뉴 표시
        var mainMenu = await UIManager.Instance.ShowAsync<MainMenuUI>(
            UILayer.Overlay,
            ct: ct
        );

        if (mainMenu != null)
        {
            Debug.Log("메인 메뉴 표시 완료");
        }
    }
}
```

### 3. UI 숨기기

`UIManager.Instance.Hide<T>()`로 UI를 숨깁니다.

```csharp
public class GameStarter : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 메인 메뉴 숨기기
            UIManager.Instance.Hide<MainMenuUI>();
        }
    }
}
```

### 4. 데이터 전달하기

UI에 데이터를 전달할 수 있습니다.

```csharp
// 데이터 클래스 정의
public class RewardData
{
    public int Gold;
    public int Exp;
    public string ItemName;
}

// UI 클래스 (타입 안전)
[PoolAddress("UI/RewardPopup")]
public class RewardPopupUI : UIBase<RewardData>
{
    public override UILayer Layer => UILayer.PopUp;

    public Text goldText;
    public Text expText;
    public Text itemText;

    // 타입 안전한 초기화
    public override void OnInitialize(RewardData data)
    {
        if (data != null)
        {
            goldText.text = $"골드: {data.Gold}";
            expText.text = $"경험치: {data.Exp}";
            itemText.text = $"아이템: {data.ItemName}";
        }
    }
}

// 사용
public class RewardSystem : MonoBehaviour
{
    private async UniTask ShowRewardAsync(CancellationToken ct)
    {
        var data = new RewardData
        {
            Gold = 1000,
            Exp = 500,
            ItemName = "전설의 검"
        };

        // 데이터와 함께 표시
        await UIManager.Instance.ShowAsync<RewardPopupUI>(
            UILayer.PopUp,
            data: data,
            ct: ct
        );
    }
}
```

### 5. Dim(배경 어둡게) 사용

팝업 뒤 배경을 어둡게 만들 수 있습니다.

```csharp
// Dim 사용
await UIManager.Instance.ShowAsync<ConfirmPopupUI>(
    UILayer.PopUp,
    useDim: true,  // Dim 활성화
    ct: ct
);
```

### 6. UI 애니메이션

ShowAnimation과 HideAnimation을 지정할 수 있습니다.

```csharp
[PoolAddress("UI/FadePopup")]
public class FadePopupUI : UIBase
{
    public override UILayer Layer => UILayer.PopUp;

    // 페이드 인 애니메이션 (ShowAnimation)
    public override UIAnimation ShowAnimation => new UIFadeIn(duration: 0.3f);

    // 페이드 아웃 애니메이션 (HideAnimation)
    public override UIAnimation HideAnimation => new UIFadeOut(duration: 0.3f);
}
```

### 7. 현재 표시 중인 UI 가져오기

```csharp
// UI가 표시 중인지 확인
if (UIManager.Instance.IsShowing<MainMenuUI>())
{
    // 표시 중인 UI 가져오기
    var mainMenu = UIManager.Instance.Get<MainMenuUI>();

    if (mainMenu != null)
    {
        // UI 조작
    }
}
```

---

## 실전 예제

### 인벤토리 UI

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

// 인벤토리 데이터
public class InventoryData
{
    public List<string> Items;
}

[PoolAddress("UI/Inventory")]
public class InventoryUI : UIBase<InventoryData>
{
    public override UILayer Layer => UILayer.Overlay;

    public Transform itemContainer;
    public GameObject itemSlotPrefab;
    public Button closeButton;

    private List<GameObject> itemSlots = new List<GameObject>();

    public override void OnInitialize(InventoryData data)
    {
        // 닫기 버튼
        closeButton.onClick.AddListener(() =>
        {
            UIManager.Instance.Hide<InventoryUI>();
        });

        // 아이템 표시
        if (data != null && data.Items != null)
        {
            foreach (var item in data.Items)
            {
                var slot = Instantiate(itemSlotPrefab, itemContainer);
                slot.GetComponentInChildren<Text>().text = item;
                itemSlots.Add(slot);
            }
        }
    }

    public override async UniTask OnHideAsync(CancellationToken ct)
    {
        // 아이템 슬롯 정리
        foreach (var slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();

        await UniTask.CompletedTask;
    }
}

// 사용 예시
public class InventoryController : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            OpenInventoryAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private async UniTaskVoid OpenInventoryAsync(CancellationToken ct)
    {
        var data = new InventoryData
        {
            Items = new List<string> { "체력 포션", "마나 포션", "철검", "가죽 갑옷" }
        };

        await UIManager.Instance.ShowAsync<InventoryUI>(
            UILayer.Overlay,
            data: data,
            ct: ct
        );
    }
}
```

### 확인 다이얼로그

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

// 다이얼로그 데이터
public class DialogData
{
    public string Title;
    public string Message;
    public Action OnConfirm;
    public Action OnCancel;
}

[PoolAddress("UI/ConfirmDialog")]
public class ConfirmDialogUI : UIBase<DialogData>
{
    public override UILayer Layer => UILayer.PopUp;

    public Text titleText;
    public Text messageText;
    public Button confirmButton;
    public Button cancelButton;

    private Action onConfirm;
    private Action onCancel;

    public override void OnInitialize(DialogData data)
    {
        if (data != null)
        {
            titleText.text = data.Title;
            messageText.text = data.Message;
            onConfirm = data.OnConfirm;
            onCancel = data.OnCancel;
        }

        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnConfirmClicked()
    {
        onConfirm?.Invoke();
        UIManager.Instance.Hide<ConfirmDialogUI>();
    }

    private void OnCancelClicked()
    {
        onCancel?.Invoke();
        UIManager.Instance.Hide<ConfirmDialogUI>();
    }
}

// 사용 예시
public class GameManager : MonoBehaviour
{
    public async UniTask ShowQuitConfirmAsync(CancellationToken ct)
    {
        var data = new DialogData
        {
            Title = "게임 종료",
            Message = "정말로 게임을 종료하시겠습니까?",
            OnConfirm = () =>
            {
                Debug.Log("게임 종료");
                Application.Quit();
            },
            OnCancel = () =>
            {
                Debug.Log("취소");
            }
        };

        await UIManager.Instance.ShowAsync<ConfirmDialogUI>(
            UILayer.PopUp,
            data: data,
            useDim: true,  // 배경 어둡게
            ct: ct
        );
    }
}
```

### HUD (항상 표시)

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;

[PoolAddress("UI/GameHUD")]
public class GameHUDUI : UIBase
{
    public override UILayer Layer => UILayer.HUD;

    // 씬 전환 시에도 유지
    public override bool DestroyOnSceneChange => false;

    public Text healthText;
    public Text scoreText;
    public Slider healthBar;

    private int currentHealth;
    private int maxHealth = 100;
    private int score;

    public override void OnInitialize(object data)
    {
        UpdateHealth(maxHealth);
        UpdateScore(0);
    }

    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        Debug.Log("HUD 표시");
        await UniTask.CompletedTask;
    }

    public void UpdateHealth(int health)
    {
        currentHealth = health;
        healthText.text = $"HP: {currentHealth}/{maxHealth}";
        healthBar.value = (float)currentHealth / maxHealth;
    }

    public void UpdateScore(int newScore)
    {
        score = newScore;
        scoreText.text = $"Score: {score}";
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealth(currentHealth);
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScore(score);
    }
}

// 사용 예시
public class Player : MonoBehaviour
{
    private GameHUDUI hud;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // HUD 표시
        hud = await UIManager.Instance.ShowAsync<GameHUDUI>(
            UILayer.HUD,
            ct: ct
        );
    }

    public void OnDamaged(int damage)
    {
        if (hud != null)
        {
            hud.TakeDamage(damage);
        }
    }

    public void OnScoreChanged(int points)
    {
        if (hud != null)
        {
            hud.AddScore(points);
        }
    }
}
```

### 로딩 화면

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;

[PoolAddress("UI/LoadingScreen")]
public class LoadingScreenUI : UIBase
{
    public override UILayer Layer => UILayer.Transition;

    public Slider progressBar;
    public Text loadingText;

    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        progressBar.value = 0f;
        loadingText.text = "로딩 중...";
        await UniTask.CompletedTask;
    }

    public void UpdateProgress(float progress, string message = null)
    {
        progressBar.value = progress;

        if (!string.IsNullOrEmpty(message))
        {
            loadingText.text = message;
        }
    }
}

// 사용 예시
public class SceneLoader : MonoBehaviour
{
    public async UniTask LoadSceneAsync(string sceneName, CancellationToken ct)
    {
        // 로딩 화면 표시
        var loadingScreen = await UIManager.Instance.ShowAsync<LoadingScreenUI>(
            UILayer.Transition,
            ct: ct
        );

        // 씬 로드
        var operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            loadingScreen.UpdateProgress(progress, $"로딩 중... {(int)(progress * 100)}%");

            await UniTask.Yield(ct);
        }

        // 로딩 화면 숨김
        UIManager.Instance.Hide<LoadingScreenUI>();
    }
}
```

### 토스트 메시지

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;
using System.Threading;

public class ToastData
{
    public string Message;
    public float Duration = 2f;
}

[PoolAddress("UI/Toast")]
public class ToastUI : UIBase<ToastData>
{
    public override UILayer Layer => UILayer.System;

    public Text messageText;

    public override void OnInitialize(ToastData data)
    {
        if (data != null)
        {
            messageText.text = data.Message;

            // 일정 시간 후 자동으로 숨김
            AutoHideAsync(data.Duration, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private async UniTaskVoid AutoHideAsync(float duration, CancellationToken ct)
    {
        await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
        UIManager.Instance.Hide<ToastUI>();
    }
}

// 사용 예시
public class ToastHelper : MonoBehaviour
{
    public static async UniTask ShowToastAsync(string message, float duration = 2f, CancellationToken ct = default)
    {
        var data = new ToastData
        {
            Message = message,
            Duration = duration
        };

        await UIManager.Instance.ShowAsync<ToastUI>(
            UILayer.System,
            data: data,
            ct: ct
        );
    }
}

// 호출 예시
ToastHelper.ShowToastAsync("아이템을 획득했습니다!", ct: ct).Forget();
```

---

## 고급 기능

### 특정 레이어의 모든 UI 숨기기

```csharp
// PopUp 레이어의 모든 UI 숨기기
UIManager.Instance.HideAll(UILayer.PopUp);

// 즉시 숨기기 (애니메이션 스킵)
UIManager.Instance.HideAll(UILayer.PopUp, immediate: true);
```

### 뒤로가기 스택

PopUp 레이어는 자동으로 스택에 추가됩니다.

```csharp
// ESC 키로 최상단 팝업 닫기
private void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        UIManager.Instance.HandleBackKey();
    }
}
```

### 해상도 변경 대응

```csharp
[PoolAddress("UI/ResponsiveUI")]
public class ResponsiveUI : UIBase
{
    public override UILayer Layer => UILayer.Overlay;

    // 해상도 변경 시 호출
    public override void OnResolutionChanged(Vector2Int newResolution)
    {
        Debug.Log($"해상도 변경: {newResolution.x} x {newResolution.y}");

        // UI 요소 재배치 등
    }
}
```

---

## 주의사항

### ⚠️ 반드시 PoolAddress Attribute 추가

```csharp
// ❌ 나쁜 예: Attribute 없음
public class MyUI : UIBase
{
    // ShowAsync 호출 시 오류!
}

// ✅ 좋은 예: Attribute 추가
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase
{
}
```

### ⚠️ Layer 속성 필수 구현

```csharp
// ❌ 나쁜 예: Layer 미구현
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase
{
    // Layer 구현 안 함 → 컴파일 오류!
}

// ✅ 좋은 예: Layer 구현
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase
{
    public override UILayer Layer => UILayer.Overlay;
}
```

### ⚠️ OnInitialize vs OnShowAsync

```csharp
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase
{
    // OnInitialize: 최초 1회만 호출 (버튼 이벤트 등록)
    public override void OnInitialize(object data)
    {
        button.onClick.AddListener(OnClicked);
    }

    // OnShowAsync: 표시될 때마다 호출 (데이터 갱신)
    public override async UniTask OnShowAsync(CancellationToken ct)
    {
        UpdateUI();
        await UniTask.CompletedTask;
    }
}
```

### ⚠️ 이미 표시 중인 UI 다시 Show

```csharp
// 이미 표시 중이면 기존 인스턴스 반환
var ui1 = await UIManager.Instance.ShowAsync<MyUI>(UILayer.Overlay, ct: ct);
var ui2 = await UIManager.Instance.ShowAsync<MyUI>(UILayer.Overlay, ct: ct);

// ui1과 ui2는 동일한 인스턴스
Debug.Log(ui1 == ui2); // True
```

### ⚠️ Addressable 프리팹 설정

UI 프리팹을 Addressables에 등록해야 합니다.

1. UI 프리팹을 Addressables Groups에 추가
2. Address를 `PoolAddress`와 동일하게 설정
   - 예: `[PoolAddress("UI/MainMenu")]` → Address: `UI/MainMenu`

### ⚠️ null 체크

```csharp
// ❌ 나쁜 예: null 체크 없음
var ui = await UIManager.Instance.ShowAsync<MyUI>(UILayer.Overlay, ct: ct);
ui.DoSomething(); // NullReferenceException 가능!

// ✅ 좋은 예: null 체크
var ui = await UIManager.Instance.ShowAsync<MyUI>(UILayer.Overlay, ct: ct);
if (ui != null)
{
    ui.DoSomething();
}
```

---

## FAQ

### Q1. UI를 언제 사용하나요?

A. 모든 게임 UI는 이 시스템을 사용합니다:
- ✅ 메뉴, 인벤토리, 상점
- ✅ HUD (체력바, 미니맵 등)
- ✅ 팝업, 다이얼로그
- ✅ 로딩 화면
- ✅ 토스트 메시지

### Q2. UI가 표시되지 않습니다.

A. 다음을 확인하세요:
1. `[PoolAddress]` Attribute가 있는지
2. Addressables에 UI 프리팹이 등록되어 있는지
3. Address가 `PoolAddress`와 일치하는지
4. `Layer` 속성을 구현했는지

### Q3. 데이터 전달 시 타입이 다릅니다.

A. `UIBase<TData>`를 사용하면 타입 안전하게 전달할 수 있습니다.

```csharp
// 타입 안전
public class MyUI : UIBase<MyData>
{
    public override void OnInitialize(MyData data)
    {
        // data는 MyData 타입 보장
    }
}
```

### Q4. UI 애니메이션은 어떻게 만드나요?

A. `UIAnimation` 클래스를 상속받아 커스텀 애니메이션을 만들 수 있습니다.

```csharp
public override UIAnimation ShowAnimation => new UIFadeIn(duration: 0.3f);
public override UIAnimation HideAnimation => new UISlideOut(direction: Vector2.down, duration: 0.2f);
```

자세한 내용은 `UIAnimation` 클래스 참조.

### Q5. UI를 씬 전환 후에도 유지하려면?

A. `DestroyOnSceneChange`를 `false`로 설정합니다.

```csharp
public override bool DestroyOnSceneChange => false;
```

### Q6. 여러 UI를 동시에 표시할 수 있나요?

A. 네, 가능합니다. 각 UI 타입당 하나씩 표시할 수 있습니다.

```csharp
await UIManager.Instance.ShowAsync<MenuUI>(UILayer.Overlay, ct: ct);
await UIManager.Instance.ShowAsync<InventoryUI>(UILayer.Overlay, ct: ct);
await UIManager.Instance.ShowAsync<ConfirmUI>(UILayer.PopUp, ct: ct);

// 3개 모두 동시에 표시됨
```

### Q7. UI Pool 크기는 어떻게 설정하나요?

A. PoolManager 설정에 따릅니다. 기본값은 타입당 10개입니다.

대부분의 UI는 1개만 있어도 충분하므로 기본값 사용 권장.

---

## 요약

**UI 시스템 사용 3단계:**

1. **UIBase 상속 + PoolAddress Attribute**
2. **ShowAsync로 표시**
3. **Hide로 숨기기**

```csharp
// 1. UI 클래스 작성
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase
{
    public override UILayer Layer => UILayer.Overlay;

    public override void OnInitialize(object data)
    {
        // 초기화
    }
}

// 2. 표시
var ui = await UIManager.Instance.ShowAsync<MyUI>(
    UILayer.Overlay,
    ct: ct
);

// 3. 숨기기
UIManager.Instance.Hide<MyUI>();
```

**데이터 전달 시:**
```csharp
// UI 클래스
[PoolAddress("UI/MyUI")]
public class MyUI : UIBase<MyData>
{
    public override UILayer Layer => UILayer.Overlay;

    public override void OnInitialize(MyData data)
    {
        // 타입 안전한 초기화
    }
}

// 사용
var data = new MyData { Value = 100 };
await UIManager.Instance.ShowAsync<MyUI>(
    UILayer.Overlay,
    data: data,
    ct: ct
);
```

**핵심 원칙:**
- 모든 UI는 UIBase 상속
- PoolAddress Attribute 필수
- Layer 지정 필수
- Pool 시스템으로 자동 재사용
- Addressable로 리소스 관리

**추가 정보:**
- 소스 코드: `Assets/Scripts/Common/UI/Core/UIManager.cs`
- 기본 클래스: `Assets/Scripts/Common/UI/Core/UIBase.cs`
- 레이어 정의: `Assets/Scripts/Common/UI/Core/UILayer.cs`
