# CSV Parser 시스템 가이드

## 개요

CSV Parser 시스템은 CSV 파일을 기반으로 게임 데이터를 관리하는 스키마 기반 데이터 시스템입니다. 스키마 정의 파일로부터 C# 클래스를 자동 생성하고, 테이블 간 참조를 자동으로 해결하며, Addressables를 통한 비동기 로딩을 지원합니다.

### 주요 특징
- ✅ **스키마 기반 자동 코드 생성**: _Schema.csv 파일로부터 C# 클래스 자동 생성
- ✅ **테이블 간 참조 지원**: ID를 통한 크로스 테이블 참조 자동 해결
- ✅ **순환 참조 검사**: 초기화 단계에서 자동으로 순환 참조 탐지 및 오류 보고
- ✅ **복합 타입 지원**: 배열, 리스트, 딕셔너리, Enum, 커스텀 클래스 지원
- ✅ **Dirty Check**: 변경된 스키마만 선택적으로 재생성
- ✅ **Addressables 자동 등록**: CSV 파일을 자동으로 Addressables에 등록
- ✅ **UniTask 기반 비동기 로딩**: 성능 최적화된 비동기 파싱
- ✅ **플랫폼 독립적**: 모든 Unity 지원 플랫폼에서 동작

---

## 핵심 개념

### 1. 스키마 파일 구조

각 데이터 테이블은 두 개의 CSV 파일로 구성됩니다:

#### `[TableName]_Schema.csv` - 스키마 정의
```
ColumnName,Type,Description,Reference
ID,int,고유 ID,
Name,string,이름,
HP,int,체력,
CategoryID,int,카테고리 ID,CategoryData.ID
```

#### `[TableName].csv` - 실제 데이터
```
ID,Name,HP,CategoryID
1,체력 포션,0,1
2,마나 포션,0,1
3,철검,50,2
```

### 2. 스키마 컬럼 정의

| 컬럼 | 설명 | 예시 |
|------|------|------|
| **ColumnName** | C# 클래스의 필드명 | `ID`, `Name`, `HP` |
| **Type** | 데이터 타입 | `int`, `float`, `string`, `bool`, `int?`, `float?`, `bool?` |
| **Description** | 필드 설명 (XML 주석으로 생성) | `고유 ID`, `아이템 이름` |
| **Reference** | 참조 테이블 (선택) | `CategoryData.ID` |

### 3. 참조 시스템

**스키마 정의:**
```
CategoryID,int,카테고리 ID,CategoryData.ID
```

**생성되는 C# 코드:**
```csharp
[CSVReference("CategoryData", "ID")]
public CategoryData Category;  // 참조 객체 (CategoryID → Category)

public int CategoryID;  // 원본 ID 필드
```

**사용 예시:**
```csharp
ItemData item = CSVManager.Instance.GetTable<ItemData>()[0];
Debug.Log(item.Category.Name);  // 참조된 CategoryData의 Name 직접 접근
```

---

## 설치 및 설정

### 1. 폴더 구조
```
Assets/
├── Data/
│   └── CSV/                          # CSV 파일 저장 위치
│       ├── CategoryData.csv
│       ├── CategoryData_Schema.csv
│       ├── ItemData.csv
│       └── ItemData_Schema.csv
├── Scripts/
│   ├── Data/
│   │   └── Generated/                # 자동 생성된 C# 클래스
│   │       ├── CategoryData.cs
│   │       └── ItemData.cs
│   └── Core/
│       └── CSV/
│           ├── Runtime/              # 런타임 스크립트
│           └── Editor/               # 에디터 전용 스크립트
```

### 2. 초기 설정

1. **스키마 파일 작성**
   - `Assets/Data/CSV/` 폴더에 `[TableName]_Schema.csv` 파일 생성
   - 위의 스키마 구조에 맞춰 컬럼 정의

2. **데이터 파일 작성**
   - 같은 폴더에 `[TableName].csv` 파일 생성
   - 스키마에 정의된 컬럼 순서대로 데이터 입력

3. **C# 클래스 생성**
   - Unity 메뉴: `Tools > CSV > Generate Scripts (Dirty Check)`
   - 또는 강제 재생성: `Tools > CSV > Force Regenerate All`

4. **Addressables 자동 등록**
   - 클래스 생성 시 자동으로 "CSV Data" 그룹에 등록됨
   - 수동 설정 불필요

---

## 기본 사용법

### 1. 스키마 작성

**CategoryData_Schema.csv:**
```
ColumnName,Type,Description,Reference
ID,int,카테고리 고유 ID,
Name,string,카테고리 이름,
IconPath,string,아이콘 경로,
```

**ItemData_Schema.csv:**
```
ColumnName,Type,Description,Reference
ID,int,아이템 고유 ID,
Name,string,아이템 이름,
CategoryID,int,카테고리 ID,CategoryData.ID
Price,int,가격,
IsStackable,bool,중첩 가능 여부,
```

### 2. 데이터 작성

**CategoryData.csv:**
```
ID,Name,IconPath
1,소모품,Icons/Consumable
2,장비,Icons/Equipment
3,재료,Icons/Material
```

**ItemData.csv:**
```
ID,Name,CategoryID,Price,IsStackable
1,체력 포션,1,50,true
2,마나 포션,1,80,true
3,철검,2,500,false
4,가죽 갑옷,2,300,false
```

### 3. 클래스 생성

Unity 메뉴에서 `Tools > CSV > Generate Scripts (Dirty Check)` 실행

**생성된 CategoryData.cs:**
```csharp
// Auto-Generated from CategoryData_Schema.csv
// 수정하지 마세요!

using System;

[Serializable]
[CSVTable("CategoryData")]
public class CategoryData : ICSVData
{
    /// <summary>
    /// 카테고리 고유 ID
    /// </summary>
    public int ID;

    /// <summary>
    /// 카테고리 이름
    /// </summary>
    public string Name;

    /// <summary>
    /// 아이콘 경로
    /// </summary>
    public string IconPath;
}
```

**생성된 ItemData.cs:**
```csharp
// Auto-Generated from ItemData_Schema.csv
// 수정하지 마세요!

using System;

[Serializable]
[CSVTable("ItemData")]
public class ItemData : ICSVData
{
    /// <summary>
    /// 아이템 고유 ID
    /// </summary>
    public int ID;

    /// <summary>
    /// 아이템 이름
    /// </summary>
    public string Name;

    /// <summary>
    /// 카테고리 ID
    /// </summary>
    [CSVReference("CategoryData", "ID")]
    public CategoryData Category;

    public int CategoryID;

    /// <summary>
    /// 가격
    /// </summary>
    public int Price;

    /// <summary>
    /// 중첩 가능 여부
    /// </summary>
    public bool IsStackable;
}
```

### 4. 런타임 사용

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        // CSV 데이터 초기화 (모든 테이블 로드 + 참조 해결)
        await CSVManager.Instance.Initialize(this.GetCancellationTokenOnDestroy());

        // 데이터 사용
        List<ItemData> items = CSVManager.Instance.GetTable<ItemData>();

        foreach (ItemData item in items)
        {
            // 참조가 자동으로 해결되어 있음
            Debug.Log($"아이템: {item.Name}, 카테고리: {item.Category.Name}");
        }
    }
}
```

---

## 고급 기능

### 1. Nullable 타입 지원

스키마에서 `int?`, `float?`, `bool?` 타입을 사용하면 null 값을 허용합니다.

**스키마:**
```
ColumnName,Type,Description,Reference
Level,int?,요구 레벨 (선택),
```

**CSV 데이터:**
```
ID,Name,Level
1,초보자 검,,      # Level은 null
2,고급 검,10
```

**생성된 코드:**
```csharp
public int? Level;
```

### 2. Parse Mode

CSVParser는 두 가지 파싱 모드를 지원합니다:

```csharp
// Lenient 모드 (기본): 파싱 실패 시 기본값 사용
await CSVParser.ParseAsync<ItemData>("ItemData", cancellationToken, ParseMode.Lenient);

// Strict 모드: 파싱 실패 시 예외 발생
await CSVParser.ParseAsync<ItemData>("ItemData", cancellationToken, ParseMode.Strict);
```

### 3. 특정 테이블만 로드

```csharp
// 전체 초기화 없이 특정 테이블만 로드
List<ItemData> items = await CSVParser.ParseAsync<ItemData>("ItemData", cancellationToken);
```

### 4. Dirty Check 시스템

CSVCodeGenerator는 파일 타임스탬프를 비교하여 변경된 스키마만 재생성합니다:

- `*_Schema.csv` 파일이 `.cs` 파일보다 최신일 경우에만 재생성
- 변경 없으면 스킵하여 빌드 시간 단축
- 강제 재생성이 필요한 경우: `Tools > CSV > Force Regenerate All`

---

## 주의사항

### ⚠️ 생성된 코드 직접 수정 금지

자동 생성된 `Assets/Scripts/Data/Generated/` 폴더의 `.cs` 파일은 절대 직접 수정하지 마세요.

```csharp
// ❌ 잘못된 방법: ItemData.cs 파일을 직접 수정
// 다음 생성 시 모든 변경사항이 사라집니다!

// ✅ 올바른 방법: ItemData_Schema.csv를 수정 후 재생성
```

### ⚠️ CSV 파일 인코딩

CSV 파일은 **UTF-8 (BOM 포함)** 인코딩으로 저장해야 합니다.

- Excel에서 CSV 저장 시 인코딩 주의
- 한글이 깨지는 경우 UTF-8 BOM으로 다시 저장
- 추천 편집기: Visual Studio Code (UTF-8 BOM 저장 지원)

### ⚠️ 순환 참조 (Circular Reference)

**순환 참조란?**

테이블 간 참조가 순환 구조를 이루는 것을 말합니다.

```
예시: A → B → C → A
```

**시스템 동작:**

CSV Parser는 초기화 단계에서 자동으로 순환 참조를 검사합니다.

```csharp
// CSVManager.Initialize() 호출 시 자동 검사
await CSVManager.Instance.Initialize(cancellationToken);
```

**순환 참조 발견 시:**

```
[CSVManager] 순환 참조 감지!
순환 경로: ItemData → CategoryData → SubCategory → ItemData

테이블 간 참조가 순환을 이루고 있습니다. 스키마를 수정하여 순환을 제거해주세요.
```

초기화가 중단되고 `InvalidOperationException` 예외가 발생합니다.

**허용되는 참조:**

```
✅ 일방향 참조: ItemData → CategoryData → MainCategory
✅ 여러 테이블이 같은 테이블 참조: ItemData → CategoryData ← QuestData
✅ 자기 자신 참조 (트리 구조): CategoryData → CategoryData (ParentID)
```

**허용되지 않는 참조:**

```
❌ 양방향 순환: ItemData → CategoryData → ItemData
❌ 다단계 순환: A → B → C → D → A
❌ 복잡한 순환: A → B, A → C, C → B, B → A
```

**해결 방법:**

순환 참조를 제거하려면 다음 방법을 사용하세요:

1. **참조 방향 통일**: 한쪽 방향으로만 참조하도록 설계
2. **중간 테이블 활용**: 다대다 관계는 중간 매핑 테이블 사용
3. **ID만 저장**: 역방향 참조가 필요하면 ID만 저장하고 코드에서 찾기

```csharp
// 예시: CategoryData에서 해당 카테고리의 아이템들 찾기
CategoryData category = ...;
List<ItemData> items = CSVManager.Instance.GetTable<ItemData>();
List<ItemData> categoryItems = items.FindAll(item => item.CategoryID == category.ID);
```

### ⚠️ ID 컬럼 필수

참조를 사용하는 테이블은 반드시 고유한 ID 컬럼을 가져야 합니다.

```csv
ID,Name,CategoryID
1,체력 포션,1
2,마나 포션,1
```

### ⚠️ Addressables 그룹 관리

CSV 파일은 자동으로 "CSV Data" 그룹에 등록됩니다.

- 수동으로 그룹을 삭제하지 마세요
- 그룹 이름 변경이 필요한 경우 `CSVCodeGenerator.cs`의 `ADDRESSABLES_GROUP_NAME` 상수 수정

### ⚠️ UniTask 의존성

본 시스템은 UniTask를 사용합니다. 프로젝트에 UniTask가 설치되어 있어야 합니다.

### ⚠️ CSV 인젝션 방지

CSVParser는 `=`, `+`, `-`, `@`로 시작하는 값을 자동으로 제거합니다 (CSV Injection 방지).

```csv
ID,Name,Formula
1,체력 포션,=1+1     # 파싱 시 "1+1"로 변환됨
```

실제로 `=`로 시작하는 값이 필요한 경우 작은따옴표로 이스케이프:
```csv
ID,Name,Value
1,수식,'=1+1        # "=1+1"로 파싱됨
```

### ⚠️ 성능 고려사항

```csharp
// ❌ 나쁜 예: 매번 초기화 호출
void Update()
{
    await CSVManager.Instance.Initialize(cancellationToken); // 매우 비효율적!
}

// ✅ 좋은 예: 게임 시작 시 한 번만 초기화
async void Start()
{
    await CSVManager.Instance.Initialize(this.GetCancellationTokenOnDestroy());
}
```

- **Reflection 캐싱**: CSVParser는 내부적으로 리플렉션 정보를 캐싱하므로 동일 타입 재파싱 시 성능이 향상됩니다.
- **대용량 데이터**: 10,000개 이상의 레코드를 가진 테이블은 로딩 시간이 길어질 수 있습니다.
- **참조 해결 비용**: 참조가 많을수록 초기화 시간이 증가합니다. 불필요한 참조는 피하세요.

### ✅ 지원 타입

CSV Parser는 다양한 데이터 타입을 지원합니다:

#### 기본 타입
- `int`, `float`, `bool`, `string`
- `int?`, `float?`, `bool?` (Nullable 타입)

#### Enum 타입
```csharp
public enum ItemType
{
    Consumable,
    Equipment,
    Material
}
```

**스키마:**
```csv
ColumnName,Type,Description,Reference
ItemType,ItemType,아이템 종류,
```

**CSV 데이터:**
```csv
ID,Name,ItemType
1,체력 포션,Consumable
2,철검,Equipment
```

#### 배열 (Array)
**형식**: 세미콜론(`;`)으로 구분

**스키마:**
```csv
ColumnName,Type,Description,Reference
RequiredLevels,int[],요구 레벨 목록,
```

**CSV 데이터:**
```csv
ID,Name,RequiredLevels
1,고급 검,"10;20;30"
2,초보자 검,"1;5"
```

**사용:**
```csharp
ItemData item = CSVManager.Instance.GetTable<ItemData>()[0];
foreach (int level in item.RequiredLevels)
{
    Debug.Log($"요구 레벨: {level}");
}
```

#### 리스트 (List)
**형식**: 배열과 동일하게 세미콜론(`;`)으로 구분

**스키마:**
```csv
ColumnName,Type,Description,Reference
Tags,List<string>,태그 목록,
```

**CSV 데이터:**
```csv
ID,Name,Tags
1,마법검,"magic;rare;weapon"
2,철갑옷,"armor;common"
```

**사용:**
```csharp
ItemData item = CSVManager.Instance.GetTable<ItemData>()[0];
item.Tags.Add("legendary"); // List는 동적으로 추가 가능
```

#### 딕셔너리 (Dictionary)
**형식**: `key:value;key:value` 형식

**스키마:**
```csv
ColumnName,Type,Description,Reference
Stats,Dictionary<string;int>,스탯 정보,
```

**CSV 데이터:**
```csv
ID,Name,Stats
1,철검,"damage:50;speed:10;durability:100"
2,목검,"damage:10;speed:20;durability:30"
```

**사용:**
```csharp
ItemData item = CSVManager.Instance.GetTable<ItemData>()[0];
Debug.Log($"데미지: {item.Stats["damage"]}");
Debug.Log($"속도: {item.Stats["speed"]}");
```

#### 커스텀 클래스/구조체
**형식**: JSON 문자열 (Unity JsonUtility 사용)

**커스텀 타입 정의:**
```csharp
[Serializable]
public struct ItemPosition
{
    public float x;
    public float y;
    public float z;
}
```

**스키마:**
```csv
ColumnName,Type,Description,Reference
Position,ItemPosition,아이템 위치,
```

**CSV 데이터:**
```csv
ID,Name,Position
1,보물상자,"{""x"":10.5,""y"":20.3,""z"":0}"
2,NPC,"{""x"":5.0,""y"":0,""z"":10.2}"
```

**주의사항:**
- 커스텀 타입은 반드시 `[Serializable]` 속성 필요
- JSON 형식 준수 (큰따옴표는 `""` 로 이스케이프)
- Unity의 JsonUtility 제약사항 적용 (프로퍼티 불가, public 필드만 가능)

**사용:**
```csharp
ItemData item = CSVManager.Instance.GetTable<ItemData>()[0];
Debug.Log($"위치: ({item.Position.x}, {item.Position.y}, {item.Position.z})");
```

### ⚠️ 지원하지 않는 타입

다음 타입들은 현재 지원하지 않습니다:

- ❌ **중첩된 복합 타입**: `List<List<int>>`, `Dictionary<int, List<string>>` 등
- ❌ **다차원 배열**: `int[,]`, `int[][]`
- ❌ **Tuple**: `(int, string)`
- ❌ **HashSet, Queue, Stack** 등의 컬렉션

이러한 타입이 필요한 경우, JSON 형식의 커스텀 클래스로 대체하는 것을 권장합니다.

### ⚠️ 에디터 전용 도구

`CSVCodeGenerator`와 `CSVEditorMenu`는 에디터 전용입니다.

- `#if UNITY_EDITOR` 디렉티브로 보호됨
- 빌드에 포함되지 않음
- Editor 폴더에 위치해야 함

### ⚠️ CancellationToken 필수

```csharp
// ❌ 나쁜 예: CancellationToken 없음
async void Start()
{
    // 컴포넌트가 파괴되어도 취소되지 않음 → 메모리 누수 위험
    await CSVManager.Instance.Initialize();
}

// ✅ 좋은 예: CancellationToken 전달
async void Start()
{
    await CSVManager.Instance.Initialize(this.GetCancellationTokenOnDestroy());
}
```

---

## FAQ

### Q1. Excel 파일을 직접 사용할 수 없나요?

A. 본 시스템은 플랫폼 독립성을 위해 순수 CSV만 지원합니다. Excel에서 작업 후 CSV로 내보내기를 권장합니다.

### Q2. 스키마 변경 후 에러가 발생합니다.

A. 다음을 확인하세요:
1. `Tools > CSV > Generate Scripts` 실행 여부
2. Unity Editor에서 컴파일 완료 대기
3. Addressables 그룹에 CSV 파일 등록 여부
4. 기존 생성된 `.cs` 파일 삭제 후 재생성

### Q3. 참조가 null입니다.

A. 참조 해결 실패 원인:
1. 참조되는 테이블의 ID가 존재하지 않음
2. `CSVManager.Initialize()` 호출 완료 전에 접근
3. 참조 테이블이 로드되지 않음
4. 스키마 Reference 컬럼 형식 오류 (올바른 형식: `TableName.ColumnName`)

### Q4. CSV 파일이 Addressables에 등록되지 않습니다.

A. `Tools > CSV > Generate Scripts` 실행 시 자동 등록됩니다. 수동 등록이 필요한 경우:
1. Addressables Groups 창 열기
2. "CSV Data" 그룹 확인
3. 없으면 `Generate Scripts` 재실행

### Q5. 대용량 CSV 로딩이 느립니다.

A. 최적화 방법:
1. 필요한 테이블만 개별 로드: `CSVParser.ParseAsync<T>()`
2. 데이터 분할: 큰 테이블을 여러 개로 분리
3. 로딩 화면 표시: UniTask 활용

### Q6. "순환 참조 감지!" 오류가 발생합니다.

A. 순환 참조 오류 해결:
1. 오류 메시지의 "순환 경로" 확인 (예: A → B → C → A)
2. 해당 테이블들의 스키마 파일 열기
3. Reference 컬럼에서 순환을 이루는 참조 찾기
4. 다음 중 한 가지 방법으로 해결:
   - 불필요한 참조 제거
   - 참조 방향을 한쪽으로만 설정
   - 중간 매핑 테이블 생성
5. `Tools > CSV > Generate Scripts` 재실행
6. 게임 재시작

### Q7. 배열/리스트 데이터가 제대로 파싱되지 않습니다.

A. 다음을 확인하세요:
1. CSV 데이터에서 세미콜론(`;`) 구분자 사용 여부
2. 따옴표로 감싸져 있는지 확인 (예: `"1;2;3"`)
3. 스키마 타입이 정확한지 확인 (예: `int[]`, `List<string>`)
4. 요소 타입이 지원되는 기본 타입인지 확인

### Q8. 커스텀 클래스 파싱이 실패합니다.

A. 다음을 확인하세요:
1. 커스텀 클래스에 `[Serializable]` 속성이 있는지 확인
2. JSON 형식이 올바른지 확인 (Unity JsonUtility 형식)
3. 프로퍼티 대신 public 필드 사용
4. 큰따옴표를 `""` 로 이스케이프했는지 확인

---

## 요약

**CSV Parser 시스템 사용 4단계:**

1. **스키마 파일 작성** (`[TableName]_Schema.csv`)
2. **데이터 파일 작성** (`[TableName].csv`)
3. **Unity 메뉴에서 클래스 생성** (`Tools > CSV > Generate Scripts`)
4. **게임 시작 시 초기화** (`CSVManager.Instance.Initialize()`)

```csharp
// 초기화
await CSVManager.Instance.Initialize(this.GetCancellationTokenOnDestroy());

// 데이터 사용
List<ItemData> items = CSVManager.Instance.GetTable<ItemData>();
foreach (ItemData item in items)
{
    Debug.Log($"{item.Name}: {item.Price}원, 카테고리: {item.Category.Name}");
}
```

**추가 정보:**
- 스키마 파일 위치: `Assets/Data/CSV/[TableName]_Schema.csv`
- 데이터 파일 위치: `Assets/Data/CSV/[TableName].csv`
- 생성된 클래스 위치: `Assets/Scripts/Data/Generated/[TableName].cs`
- Addressables 자동 등록: "CSV Data" 그룹
