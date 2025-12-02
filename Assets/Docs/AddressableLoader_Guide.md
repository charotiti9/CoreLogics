# AddressableLoader 사용 가이드

## 개요

AddressableLoader는 Unity의 Addressable Asset System을 래핑하여 리소스 로딩을 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ 참조 카운팅으로 자동 메모리 관리
- ✅ 중복 로드 방지 (동일 Address 동시 로드 시 하나의 작업 공유)
- ✅ UniTask 기반 고성능 비동기 로딩
- ✅ 리소스 로드/해제 추적 및 디버깅 기능

---

## 기본 사용법

### 1. 단일 리소스 로드하기

가장 기본적인 사용법입니다. 리소스를 로드하고 사용 후 반드시 해제합니다.

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Core.Addressable;

public class ItemIcon : MonoBehaviour
{
    private string iconAddress;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 리소스 로드
        iconAddress = "Icon_Potion";
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(iconAddress, ct);

        if (icon != null)
        {
            GetComponent<SpriteRenderer>().sprite = icon;
        }
    }

    private void OnDestroy()
    {
        // 리소스 해제 (필수!)
        AddressableLoader.Instance.Release(iconAddress);
    }
}
```

**핵심 포인트:**
- `LoadAssetAsync<T>()`: 리소스를 비동기로 로드
- `Release()`: 사용 완료 후 반드시 호출 (참조 카운트 감소)
- `CancellationToken`: 컴포넌트 파괴 시 자동 취소

### 2. 여러 종류의 리소스 로드하기

한 클래스에서 여러 리소스를 로드하는 경우입니다.

```csharp
public class Player : MonoBehaviour
{
    private string weaponAddress;
    private string armorAddress;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 무기 프리팹 로드
        weaponAddress = "Prefab_Sword";
        var weapon = await AddressableLoader.Instance.LoadAssetAsync<GameObject>(weaponAddress, ct);
        if (weapon != null)
        {
            Instantiate(weapon, transform);
        }

        // 방어구 아이콘 로드
        armorAddress = "Icon_Armor";
        var armorIcon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(armorAddress, ct);
        // armorIcon 사용...
    }

    private void OnDestroy()
    {
        // 모든 리소스 해제
        AddressableLoader.Instance.Release(weaponAddress);
        AddressableLoader.Instance.Release(armorAddress);
    }
}
```

### 3. 라벨로 여러 리소스 일괄 로드하기

같은 라벨을 가진 여러 리소스를 한 번에 로드합니다.

```csharp
public class EnemyManager : MonoBehaviour
{
    private const string ENEMY_LABEL = "Enemy"; // Addressables에서 설정한 라벨명

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // "Enemy" 라벨의 모든 프리팹 로드
        var enemyPrefabs = await AddressableLoader.Instance.LoadAssetsAsync<GameObject>(ENEMY_LABEL, ct);

        if (enemyPrefabs != null)
        {
            foreach (var prefab in enemyPrefabs)
            {
                Debug.Log($"로드된 적: {prefab.name}");
                // prefab 사용...
            }
        }
    }

    private void OnDestroy()
    {
        // 라벨로 로드한 리소스도 동일하게 해제
        AddressableLoader.Instance.Release(ENEMY_LABEL);
    }
}
```

**라벨 사용 시점:**
- 같은 카테고리의 리소스를 묶어서 관리할 때
- 스테이지별로 필요한 리소스를 그룹화할 때
- 예: "Stage1", "UI", "Enemy", "Items" 등

### 4. 프리로드 (게임 시작 시 미리 로드)

게임 시작 시 자주 사용할 리소스를 미리 로드하면 이후 즉시 사용할 수 있습니다.

```csharp
public class GameInitializer : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 방법 1: 여러 Address를 배열로 프리로드
        await AddressableLoader.Instance.PreloadAsync(new[]
        {
            "UI_MainMenu",
            "BGM_Title",
            "SFX_Click"
        }, ct);

        // 방법 2: 라벨로 일괄 프리로드
        await AddressableLoader.Instance.PreloadByLabelAsync("Essential", ct);

        Debug.Log("프리로드 완료! 게임 시작 준비 완료");
    }

    private void OnApplicationQuit()
    {
        // 프리로드한 리소스도 해제
        AddressableLoader.Instance.Release("UI_MainMenu");
        AddressableLoader.Instance.Release("BGM_Title");
        AddressableLoader.Instance.Release("SFX_Click");
        AddressableLoader.Instance.Release("Essential");
    }
}
```

**프리로드 장점:**
- 로딩 시간을 게임 시작 시점에 집중
- 게임 플레이 중 버벅임 없음
- 자주 사용하는 리소스의 즉시 접근

### 5. 동일 리소스를 여러 곳에서 사용하기

참조 카운팅 덕분에 같은 리소스를 여러 곳에서 안전하게 사용할 수 있습니다.

```csharp
// 클래스 A
public class UIHealthBar : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // Icon_Health 로드 → 참조 카운트 = 1
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
    }

    private void OnDestroy()
    {
        // 참조 카운트 = 0 → 실제 해제
        AddressableLoader.Instance.Release("Icon_Health");
    }
}

// 클래스 B (동시에 존재)
public class UIStatusPanel : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 같은 Icon_Health 로드 → 참조 카운트 = 2 (실제 로드는 하지 않고 재사용)
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
    }

    private void OnDestroy()
    {
        // 참조 카운트 = 1 (아직 UIHealthBar가 사용 중이므로 메모리 유지)
        AddressableLoader.Instance.Release("Icon_Health");
    }
}
```

**참조 카운팅 동작:**
1. 첫 로드 시 → 메모리에 올리고 참조 카운트 = 1
2. 같은 리소스 재로드 → 기존 리소스 재사용, 참조 카운트 +1
3. Release 호출 → 참조 카운트 -1
4. 참조 카운트 = 0 → 실제 메모리에서 해제

### 6. 리소스 동적 교체

게임 중 리소스를 동적으로 바꾸는 경우입니다.

```csharp
public class CharacterSkin : MonoBehaviour
{
    private string currentSkinAddress;

    public async UniTask ChangeSkinAsync(string newSkinAddress, CancellationToken ct)
    {
        // 이전 스킨 해제
        if (!string.IsNullOrEmpty(currentSkinAddress))
        {
            AddressableLoader.Instance.Release(currentSkinAddress);
        }

        // 새 스킨 로드
        var skin = await AddressableLoader.Instance.LoadAssetAsync<GameObject>(newSkinAddress, ct);
        if (skin != null)
        {
            // 스킨 적용 로직...
            currentSkinAddress = newSkinAddress;
        }
    }

    private void OnDestroy()
    {
        // 현재 스킨 해제
        if (!string.IsNullOrEmpty(currentSkinAddress))
        {
            AddressableLoader.Instance.Release(currentSkinAddress);
        }
    }
}
```

### 7. 디버깅하기

로드된 리소스를 추적하고 디버깅할 수 있습니다.

```csharp
public class DebugManager : MonoBehaviour
{
    private void Update()
    {
        // F1 키: 현재 로드된 리소스 개수 확인
        if (Input.GetKeyDown(KeyCode.F1))
        {
            int count = AddressableLoader.Instance.GetLoadedCount();
            Debug.Log($"현재 로드된 리소스: {count}개");
        }

        // F2 키: 상세 정보 출력
        if (Input.GetKeyDown(KeyCode.F2))
        {
            AddressableLoader.Instance.PrintDebugInfo();
        }

        // F3 키: 리소스 목록 확인
        if (Input.GetKeyDown(KeyCode.F3))
        {
            var assets = AddressableLoader.Instance.GetLoadedAssets();
            foreach (var asset in assets)
            {
                Debug.Log($"[{asset.AssetType.Name}] {asset.Address} (참조: {asset.ReferenceCount})");
            }
        }
    }
}
```

**PrintDebugInfo() 출력 예시:**
```
=== AddressableLoader 디버그 정보 ===
로드된 리소스: 5개
로딩 중인 작업: 0개

[로드된 리소스 목록]
- Icon_Health | Type: Sprite | RefCount: 2
- Prefab_Enemy | Type: GameObject | RefCount: 1
- BGM_Title | Type: AudioClip | RefCount: 1
- UI_MainMenu | Type: GameObject | RefCount: 1
- Icon_Armor | Type: Sprite | RefCount: 1
=====================================
```

---

## 고급 기능

### 씬 전환 시 모든 리소스 해제

씬이 바뀔 때 현재 씬의 모든 리소스를 한 번에 정리합니다.

```csharp
public class SceneTransition : MonoBehaviour
{
    public async UniTask LoadNextSceneAsync(string sceneName, CancellationToken ct)
    {
        // 현재 씬의 모든 리소스 강제 해제
        AddressableLoader.Instance.ReleaseAll();

        // 새 씬 로드
        await UnityEngine.SceneManagement.SceneManager
            .LoadSceneAsync(sceneName)
            .ToUniTask(cancellationToken: ct);
    }
}
```

**⚠️ 주의:** `ReleaseAll()`은 참조 카운트를 무시하고 모든 리소스를 강제 해제합니다. 씬 전환처럼 명확한 시점에만 사용하세요.

---

## 실전 예제

### 인벤토리 아이템 아이콘

```csharp
public class InventorySlot : MonoBehaviour
{
    private Image iconImage;
    private string currentIconAddress;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
    }

    public async UniTask SetItemAsync(string itemIconAddress, CancellationToken ct)
    {
        // 이전 아이콘 해제
        if (!string.IsNullOrEmpty(currentIconAddress))
        {
            AddressableLoader.Instance.Release(currentIconAddress);
        }

        // 새 아이콘 로드
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(itemIconAddress, ct);
        if (icon != null)
        {
            iconImage.sprite = icon;
            currentIconAddress = itemIconAddress;
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(currentIconAddress))
        {
            AddressableLoader.Instance.Release(currentIconAddress);
        }
    }
}
```

### 적 스폰 시스템

```csharp
public class EnemySpawner : MonoBehaviour
{
    private const string ENEMY_PREFAB = "Prefab_Enemy";
    private int spawnCount = 0;

    public async UniTask SpawnEnemyAsync(Vector3 position, CancellationToken ct)
    {
        // 적 프리팹 로드 (이미 로드된 경우 재사용)
        var enemyPrefab = await AddressableLoader.Instance.LoadAssetAsync<GameObject>(ENEMY_PREFAB, ct);

        if (enemyPrefab != null)
        {
            var enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
            spawnCount++;
        }
    }

    public void OnEnemyDestroyed()
    {
        if (spawnCount > 0)
        {
            // 적이 죽으면 참조 카운트 감소
            AddressableLoader.Instance.Release(ENEMY_PREFAB);
            spawnCount--;
        }
    }

    private void OnDestroy()
    {
        // 남은 참조 모두 해제
        for (int i = 0; i < spawnCount; i++)
        {
            AddressableLoader.Instance.Release(ENEMY_PREFAB);
        }
    }
}
```

### 오디오 재생

```csharp
public class AudioPlayer : MonoBehaviour
{
    private AudioSource audioSource;
    private string currentClipAddress;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public async UniTask PlayAsync(string audioAddress, CancellationToken ct)
    {
        // 이전 오디오 해제
        if (!string.IsNullOrEmpty(currentClipAddress))
        {
            AddressableLoader.Instance.Release(currentClipAddress);
        }

        // 오디오 클립 로드
        var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(audioAddress, ct);
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            currentClipAddress = audioAddress;
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(currentClipAddress))
        {
            AddressableLoader.Instance.Release(currentClipAddress);
        }
    }
}
```

---

## 주의사항

### ⚠️ 로드와 해제 짝 맞추기

```csharp
// ❌ 나쁜 예: Release를 호출하지 않음
async UniTaskVoid BadExample(CancellationToken ct)
{
    var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
    // Release 호출 안 함 → 메모리 누수!
}

// ✅ 좋은 예: 항상 Release 호출
public class GoodExample : MonoBehaviour
{
    private string iconAddress;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        iconAddress = "Icon_Health";
        var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(iconAddress, ct);
    }

    private void OnDestroy()
    {
        AddressableLoader.Instance.Release(iconAddress);
    }
}
```

### ⚠️ Address 오타 주의

```csharp
// ❌ 잘못된 예: Address 불일치
var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("icon_health", ct);
AddressableLoader.Instance.Release("Icon_Health"); // 다른 Address!

// ✅ 올바른 예: 상수 사용
public class ItemConfig
{
    public const string ICON_HEALTH = "Icon_Health";
}

var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(ItemConfig.ICON_HEALTH, ct);
AddressableLoader.Instance.Release(ItemConfig.ICON_HEALTH);
```

### ⚠️ CancellationToken 필수 전달

```csharp
// ❌ 나쁜 예: CancellationToken 없음
async UniTaskVoid Start()
{
    await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health");
    // 컴포넌트가 파괴되어도 로딩이 계속됨!
}

// ✅ 좋은 예: CancellationToken 전달
async UniTaskVoid Start()
{
    var ct = this.GetCancellationTokenOnDestroy();
    await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
}
```

### ⚠️ null 체크 필수

```csharp
// ❌ 나쁜 예: null 체크 없음
var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
image.sprite = sprite; // NullReferenceException 발생 가능!

// ✅ 좋은 예: null 체크 후 사용
var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon_Health", ct);
if (sprite != null)
{
    image.sprite = sprite;
}
else
{
    Debug.LogError("아이콘 로드 실패!");
}
```

### ⚠️ Addressables 설정 확인

리소스가 로드되지 않는 경우:

1. **Addressables Groups 창 열기**: `Window > Asset Management > Addressables > Groups`
2. 해당 리소스가 그룹에 추가되어 있는지 확인
3. **Address 이름**이 코드와 정확히 일치하는지 확인 (대소문자 구분!)
4. 처음 사용하는 경우 **Addressables 빌드**: `Build > New Build > Default Build Script`

---

## FAQ

### Q1. Resources.Load와 차이점은?

| 항목 | Resources.Load | AddressableLoader |
|------|----------------|-------------------|
| **메모리 관리** | 수동 | 자동 (참조 카운팅) |
| **빌드 크기** | 모든 Resources 폴더 포함 | 선택적 포함 |
| **중복 로드** | 방지 안 됨 | 자동 방지 |
| **성능** | 동기 (프레임 드롭) | 비동기 (부드러움) |

**권장:** Resources.Load 대신 AddressableLoader 사용

### Q2. 같은 리소스를 여러 번 로드하면?

A. 참조 카운트만 증가하고, 실제 로드는 한 번만 수행됩니다.

```csharp
// 첫 번째 로드 → 실제 로드, 참조 카운트 = 1
var icon1 = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon", ct);

// 두 번째 로드 → 재사용, 참조 카운트 = 2
var icon2 = await AddressableLoader.Instance.LoadAssetAsync<Sprite>("Icon", ct);

// icon1과 icon2는 동일한 인스턴스
```

### Q3. Release를 너무 많이 호출하면?

A. 참조 카운트는 0 이하로 내려가지 않습니다. 안전합니다.

### Q4. 로드 실패 시 어떻게 되나요?

A. `null`을 반환합니다. 예외를 던지지 않으므로 반드시 null 체크를 하세요.

### Q5. 라벨로 로드한 리소스의 개별 해제는?

A. 불가능합니다. 라벨 단위로만 해제할 수 있습니다.

```csharp
// 라벨로 로드
var items = await AddressableLoader.Instance.LoadAssetsAsync<GameObject>("Items", ct);

// 라벨 단위로만 해제 가능
AddressableLoader.Instance.Release("Items");
```

**개별 해제가 필요하면 각 Address로 따로 로드하세요.**

---

## 요약

**AddressableLoader 사용 3단계:**

1. **리소스 로드** (`LoadAssetAsync<T>()`)
2. **리소스 사용**
3. **리소스 해제** (`Release()`)

```csharp
public class BasicExample : MonoBehaviour
{
    private string resourceAddress;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 1. 로드
        resourceAddress = "Icon_Health";
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(resourceAddress, ct);

        // 2. 사용
        if (icon != null)
        {
            GetComponent<SpriteRenderer>().sprite = icon;
        }
    }

    private void OnDestroy()
    {
        // 3. 해제
        AddressableLoader.Instance.Release(resourceAddress);
    }
}
```

**핵심 원칙:**
- Load와 Release는 항상 쌍으로
- CancellationToken은 항상 전달
- null 체크는 필수
- Address는 상수로 관리

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/Addressable/AddressableLoader.cs`
- 서브시스템: `Assets/Scripts/Core/Addressable/Tracker/`
