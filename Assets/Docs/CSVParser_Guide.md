# CSVParser 사용 가이드

## 개요

CSVParser는 Addressable 시스템을 활용하여 CSV 파일을 자동으로 파싱하고 C# 클래스 리스트로 변환하는 유틸리티입니다.

**장점:**
- ✅ Addressable 기반 비동기 로딩
- ✅ 리플렉션을 활용한 자동 매핑
- ✅ 파일명만으로 간편한 로드
- ✅ 다양한 타입 자동 변환 (int, float, bool, enum 등)
- ✅ 따옴표 및 이스케이프 문자 처리

---

## 기본 사용법

### 1. CSV 파일 준비

CSV 파일을 프로젝트에 배치합니다.

**예시: `Assets/Data/CSV/Items.csv`**

```csv
ID,Name,Price,IsConsumable
1,체력 포션,100,true
2,마나 포션,150,true
3,철 검,500,false
4,강철 갑옷,1000,false
```

### 2. CSV 파일을 Addressable로 등록

1. CSV 파일 선택
2. Inspector에서 `Addressable` 체크
3. 끝! (파일 경로가 자동으로 Key가 됩니다)

**결과:**
- 파일: `Assets/Data/CSV/Items.csv`
- Addressable Key: `Assets/Data/CSV/Items.csv` ✅

### 3. 데이터 클래스 정의

CSV 헤더와 일치하는 필드/프로퍼티를 가진 클래스를 작성합니다.

```csharp
public class ItemData
{
    public int ID;
    public string Name;
    public int Price;
    public bool IsConsumable;
}
```

**중요:**
- 헤더 이름과 필드/프로퍼티 이름이 **대소문자 구분 없이** 일치해야 합니다.
- 클래스는 **기본 생성자**(`new()`)를 가져야 합니다.

### 4. CSV 파싱

```csharp
using Cysharp.Threading.Tasks;
using System.Threading;

public class ItemDatabase : MonoBehaviour
{
    private List<ItemData> items;

    async void Start()
    {
        // CancellationToken 생성 (컴포넌트 파괴 시 자동 취소)
        CancellationToken ct = this.GetCancellationTokenOnDestroy();

        // CSV 파일 로드 및 파싱 (파일명만 제공)
        items = await CSVParser.ParseAsync<ItemData>("Items", ct);

        // 결과 활용
        foreach (var item in items)
        {
            Debug.Log($"아이템: {item.Name}, 가격: {item.Price}");
        }
    }
}
```

---

## Root 경로 설정

기본 Root 경로는 `"Assets/Data/CSV"`입니다. 변경 가능합니다.

```csharp
// 게임 시작 시 한 번만 설정
void Awake()
{
    CSVParser.RootPath = "Assets/GameData/Tables";
}

// 이후 파일명만으로 로드
// 실제 로드: "Assets/GameData/Tables/Monsters.csv"
List<MonsterData> monsters = await CSVParser.ParseAsync<MonsterData>("Monsters", ct);
```

---

## 지원 타입

CSVParser는 다음 타입을 자동으로 변환합니다:

| 타입 | CSV 예시 | 설명 |
|------|----------|------|
| `int` | `123` | 정수 |
| `float` | `12.5` | 실수 |
| `double` | `12.567` | 배정밀도 실수 |
| `bool` | `true`, `false`, `1`, `0` | 불린 |
| `string` | `"Hello, World"` | 문자열 |
| `enum` | `Red`, `Green`, `Blue` | 열거형 (대소문자 무시) |

### Enum 사용 예시

```csharp
public enum ItemType
{
    Consumable,
    Equipment,
    Material
}

public class ItemData
{
    public int ID;
    public string Name;
    public ItemType Type; // enum 자동 변환
}
```

**CSV 파일:**
```csv
ID,Name,Type
1,체력 포션,Consumable
2,철 검,Equipment
3,나무,Material
```

---

## 특수 문자 처리

### 1. 쉼표가 포함된 문자열

따옴표로 감싸면 쉼표가 포함된 문자열도 처리됩니다.

```csv
ID,Name,Description
1,체력 포션,"체력을 회복합니다. 사용 시, 100 HP 회복"
2,마나 포션,"마나를 회복합니다. 사용 시, 50 MP 회복"
```

### 2. 따옴표가 포함된 문자열

이중 따옴표(`""`)를 사용하여 이스케이프합니다.

```csv
ID,Name,Description
1,희귀 아이템,"""전설의 검""이라고 불리는 아이템"
```

결과: `"전설의 검"이라고 불리는 아이템`

### 3. 빈 값 처리

빈 값은 해당 타입의 기본값으로 설정됩니다.

```csv
ID,Name,Price
1,무료 아이템,
2,유료 아이템,100
```

- `Price`가 비어있으면 → `0`
- `string`이 비어있으면 → `null`

---

## 실전 예제

### 예제 1: 게임 아이템 데이터

```csharp
public enum ItemRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class ItemData
{
    public int ID;
    public string Name;
    public string Description;
    public int Price;
    public ItemRarity Rarity;
    public bool IsStackable;
    public int MaxStack;
}

// 아이템 데이터베이스
public class ItemDatabase : MonoBehaviour
{
    private Dictionary<int, ItemData> itemDict;

    async void Awake()
    {
        await InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    private async UniTask InitializeAsync(CancellationToken ct)
    {
        // CSV 로드
        List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);

        // Dictionary로 변환 (빠른 조회)
        itemDict = new Dictionary<int, ItemData>();
        foreach (var item in items)
        {
            itemDict[item.ID] = item;
        }

        Debug.Log($"아이템 {items.Count}개 로드 완료");
    }

    public ItemData GetItem(int id)
    {
        return itemDict.TryGetValue(id, out var item) ? item : null;
    }
}
```

**CSV 파일: `Assets/Data/CSV/Items.csv`**
```csv
ID,Name,Description,Price,Rarity,IsStackable,MaxStack
1,체력 포션,체력을 100 회복합니다.,50,Common,true,99
2,마나 포션,마나를 50 회복합니다.,50,Common,true,99
3,전설의 검,"공격력 +100, 치명타 확률 +20%",5000,Legendary,false,1
4,희귀 방패,"방어력 +50, 블록 확률 +15%",2000,Epic,false,1
```

### 예제 2: 몬스터 스탯 데이터

```csharp
public class MonsterData
{
    public int ID;
    public string Name;
    public int Level;
    public float Health;
    public float Attack;
    public float Defense;
    public float MoveSpeed;
    public int ExpReward;
    public int GoldReward;
}

public class MonsterDatabase : MonoBehaviour
{
    private Dictionary<int, MonsterData> monsterDict = new Dictionary<int, MonsterData>();

    async void Awake()
    {
        await InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    private async UniTask InitializeAsync(CancellationToken ct)
    {
        List<MonsterData> monsters = await CSVParser.ParseAsync<MonsterData>("Monsters", ct);

        // Dictionary로 변환
        foreach (var monster in monsters)
        {
            monsterDict[monster.ID] = monster;
        }

        Debug.Log($"몬스터 {monsters.Count}종 로드 완료");
    }

    public MonsterData GetMonster(int id)
    {
        return monsterDict.TryGetValue(id, out var monster) ? monster : null;
    }
}
```

### 예제 3: 다국어 지원

```csharp
public class LocalizationData
{
    public string Key;
    public string Korean;
    public string English;
    public string Japanese;
}

public class LocalizationManager : MonoBehaviour
{
    private Dictionary<string, LocalizationData> localizationDict;
    private string currentLanguage = "Korean";

    async void Awake()
    {
        await InitializeAsync(this.GetCancellationTokenOnDestroy());
    }

    private async UniTask InitializeAsync(CancellationToken ct)
    {
        List<LocalizationData> data = await CSVParser.ParseAsync<LocalizationData>("Localization", ct);

        localizationDict = new Dictionary<string, LocalizationData>();
        foreach (var item in data)
        {
            localizationDict[item.Key] = item;
        }

        Debug.Log($"다국어 {data.Count}개 로드 완료");
    }

    public string GetText(string key)
    {
        if (!localizationDict.TryGetValue(key, out var data))
            return key;

        return currentLanguage switch
        {
            "Korean" => data.Korean,
            "English" => data.English,
            "Japanese" => data.Japanese,
            _ => data.Korean
        };
    }
}
```

**CSV 파일: `Assets/Data/CSV/Localization.csv`**
```csv
Key,Korean,English,Japanese
UI_Start,시작,Start,スタート
UI_Settings,설정,Settings,設定
UI_Exit,종료,Exit,終了
Item_HealthPotion,체력 포션,Health Potion,体力ポーション
```

### 예제 4: 일반 클래스에서 사용

```csharp
public class GameDataManager
{
    private CancellationTokenSource cts;
    private List<ItemData> items;

    public async UniTask InitializeAsync()
    {
        cts = new CancellationTokenSource();

        try
        {
            items = await CSVParser.ParseAsync<ItemData>("Items", cts.Token);
            Debug.Log($"아이템 {items.Count}개 로드 완료");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("로딩 취소됨");
        }
    }

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

---

## 주의사항

### ⚠️ CSV 파일을 Addressable로 등록

```csharp
// ❌ CSV 파일이 Addressable로 등록되지 않은 경우
// 에러: [CSVParser] CSV 파일을 찾을 수 없습니다: Assets/Data/CSV/Items.csv

// ✅ CSV 파일을 Addressable로 체크해야 함
// Inspector에서 Addressable 체크 → 자동으로 경로가 Key로 설정됨
```

### ⚠️ RootPath 설정

```csharp
// CSV 파일 위치: Assets/Data/CSV/Items.csv

// ❌ 잘못된 RootPath
CSVParser.RootPath = "Data/CSV"; // Assets/ 누락
List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);
// 에러: Data/CSV/Items.csv 파일을 찾을 수 없음

// ✅ 올바른 RootPath
CSVParser.RootPath = "Assets/Data/CSV"; // 기본값
List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);
// 성공: Assets/Data/CSV/Items.csv 로드
```

### ⚠️ 헤더와 필드 이름 일치

```csharp
// ❌ 나쁜 예: 헤더와 필드명 불일치
// CSV: ID,Name,Price
public class ItemData
{
    public int ItemID;  // ❌ CSV 헤더는 "ID"
    public string ItemName; // ❌ CSV 헤더는 "Name"
    public int Cost; // ❌ CSV 헤더는 "Price"
}

// ✅ 좋은 예: 헤더와 필드명 일치
public class ItemData
{
    public int ID;
    public string Name;
    public int Price;
}
```

### ⚠️ CancellationToken 필수

```csharp
// ❌ 나쁜 예: CancellationToken 없음
async void Start()
{
    // 컴포넌트가 파괴되어도 취소되지 않음 → 메모리 누수 위험
    List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items");
}

// ✅ 좋은 예: CancellationToken 전달
async void Start()
{
    CancellationToken ct = this.GetCancellationTokenOnDestroy();
    List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);
}
```

### ⚠️ 타입 변환 실패

CSV 값이 타입과 맞지 않으면 에러 로그가 출력되고 기본값이 할당됩니다.

```csv
ID,Name,Price
1,아이템,abc  # ← Price는 int인데 "abc" 입력
```

결과: `Price = 0` (기본값) + 에러 로그 출력

---

## 성능 팁

### 1. 초기화 시점에 한 번만 로드

```csharp
// ✅ 좋은 예: 게임 시작 시 한 번만 로드
public class GameDataManager : MonoBehaviour
{
    private List<ItemData> items;

    async void Awake()
    {
        CancellationToken ct = this.GetCancellationTokenOnDestroy();
        items = await CSVParser.ParseAsync<ItemData>("Items", ct);
    }
}

// ❌ 나쁜 예: 매번 로드
public async UniTask<ItemData> GetItem(int id, CancellationToken ct)
{
    // 매번 파일을 읽고 파싱하므로 비효율적
    List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);
    return items.Find(item => item.ID == id);
}
```

### 2. Dictionary로 변환하여 빠른 조회

```csharp
public class ItemDatabase : MonoBehaviour
{
    private Dictionary<int, ItemData> itemDict;

    async void Awake()
    {
        CancellationToken ct = this.GetCancellationTokenOnDestroy();
        List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);

        // O(1) 조회를 위해 Dictionary 변환
        itemDict = new Dictionary<int, ItemData>();
        foreach (var item in items)
        {
            itemDict[item.ID] = item;
        }
    }

    public ItemData GetItem(int id)
    {
        return itemDict.TryGetValue(id, out var item) ? item : null;
    }
}
```

### 3. 여러 CSV를 병렬로 로드

```csharp
async void Awake()
{
    CancellationToken ct = this.GetCancellationTokenOnDestroy();

    // 여러 CSV를 동시에 로드
    var (items, monsters, skills) = await UniTask.WhenAll(
        CSVParser.ParseAsync<ItemData>("Items", ct),
        CSVParser.ParseAsync<MonsterData>("Monsters", ct),
        CSVParser.ParseAsync<SkillData>("Skills", ct)
    );

    Debug.Log($"로드 완료: 아이템 {items.Count}, 몬스터 {monsters.Count}, 스킬 {skills.Count}");
}
```

---

## 디버깅

### 로그 메시지

CSVParser는 자동으로 로그를 출력합니다:

**성공:**
```
[CSVParser] Assets/Data/CSV/Items.csv 파싱 완료: 10개 항목
```

**에러:**
```
[CSVParser] CSV 파일을 찾을 수 없습니다: Assets/Data/CSV/Items.csv
[CSVParser] 필드/프로퍼티를 찾을 수 없습니다: ItemID (타입: ItemData)
[CSVParser] 값 변환 실패: 'abc' → Int32
```

**취소:**
```
[CSVParser] CSV 로드 취소됨: Assets/Data/CSV/Items.csv
```

---

## Addressable 설정 체크리스트

CSV 파일 로드가 실패하면 다음을 확인하세요:

1. **CSV 파일이 Addressable로 등록되었는가?**
   - CSV 파일 선택 → Inspector → `Addressable` 체크 확인

2. **파일 경로가 올바른가?**
   - 파일: `Assets/Data/CSV/Items.csv`
   - RootPath: `"Assets/Data/CSV"` (기본값)
   - 파일명: `"Items"` (확장자 제외)

3. **Addressable Key가 파일 경로와 일치하는가?**
   - Addressables Groups 창에서 Key 확인
   - Key가 `Assets/Data/CSV/Items.csv`와 일치해야 함

---

## 요약

**CSVParser 사용 3단계:**

1. **CSV 파일을 Addressable로 체크**
2. **CSV 헤더와 일치하는 데이터 클래스 작성**
3. **CSVParser.ParseAsync<T>()로 비동기 파싱**

```csharp
// 1. 데이터 클래스 정의
public class ItemData
{
    public int ID;
    public string Name;
    public int Price;
}

// 2. CSV 파싱 (비동기)
async void Start()
{
    CancellationToken ct = this.GetCancellationTokenOnDestroy();
    List<ItemData> items = await CSVParser.ParseAsync<ItemData>("Items", ct);

    // 3. 결과 활용
    foreach (var item in items)
    {
        Debug.Log($"{item.Name}: {item.Price}원");
    }
}
```

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/Utility/CSVParser.cs`
- CSV 파일 기본 위치: `Assets/Data/CSV/`
- RootPath 변경: `CSVParser.RootPath = "원하는 경로";`
