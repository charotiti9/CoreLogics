# 치트 시스템 사용 가이드

## 개요

치트 시스템은 개발 및 테스트 과정에서 사용하는 **Editor 전용** 콘솔 시스템입니다.

**장점:**
- ✅ Editor 전용 (빌드에 포함되지 않음)
- ✅ CSV 기반 치트 정의 (코드 수정 없이 추가/수정 가능)
- ✅ 리플렉션 기반 자동 치트 탐색
- ✅ 자동완성 및 파라미터 가이드 지원
- ✅ 명령어 히스토리 지원
- ✅ 큰따옴표로 공백 포함 문자열 지원

---

## 핵심 개념

### 1. 아키텍처

치트 시스템은 다음 컴포넌트로 구성됩니다:

```
┌───────────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  EditorCheatConsole   │───>│   CheatManager   │───>│  ICheat 구현체  │
│  (UI/입력 처리)       │    │ (치트 관리/실행) │    │  (실제 로직)    │
└───────────────────────┘    └──────────────────┘    └─────────────────┘
                                    │
                                    v
                             ┌──────────────────┐
                             │    CheatData     │
                             │ (CSV 데이터)     │
                             └──────────────────┘
```

### 2. 치트 정의 방식

치트는 **두 가지**를 함께 정의해야 합니다:

1. **CSV 데이터** (`CheatData.csv`): 치트 ID, 설명, 파라미터 정의
2. **구현 클래스** (`ICheat` 인터페이스 구현): 실제 실행 로직

```
CSV 데이터 (ID: "AddGold")  +  ICheat 구현 클래스 (class AddGold)
         ↓                              ↓
   UI 표시/자동완성              실제 치트 로직 실행
```

### 3. 파라미터 형식

CSV의 `Parameters` 필드는 다음 형식으로 정의합니다:

```
name:type|name:type|...
```

CSV ID를 후보 목록으로 선택하려면 아래 확장 형식을 사용합니다:

```
name:csv(TableName)
```

**예시:**
- `amount:int` - 정수형 파라미터 1개
- `itemId:string|count:int` - 문자열 + 정수 파라미터 2개
- `x:float|y:float|z:float` - 실수형 파라미터 3개

### 4. 키 바인딩

| 키 | 동작 |
|---|---|
| `` ` `` (백틱) | 치트 콘솔 열기 |
| `ESC` | 치트 콘솔 닫기 |
| `Tab` | 자동완성 |
| `↑` / `↓` | 자동완성 목록 탐색 |
| `Ctrl+↑` / `Ctrl+↓` | 히스토리 탐색 |
| `Enter` | 명령어 실행 |

---

## 기본 사용법

### 1. CSV에 치트 정의

`Assets/Data/CSV/CheatData.csv` 파일에 치트 정보를 추가합니다.

```csv
ID,Description,Parameters
AddGold,골드를 추가합니다.,amount:int
AddItem,아이템을 추가합니다.,itemId:string|count:int
SetLevel,플레이어 레벨을 설정합니다.,level:int
GodMode,무적 모드를 토글합니다.,
ClearStage,현재 스테이지를 클리어합니다.,
Teleport,지정한 위치로 이동합니다.,x:float|y:float|z:float
SpawnEnemy,적을 소환합니다.,enemyId:string|count:int|level:int
```

### 2. ICheat 구현 클래스 작성

`Assets/Scripts/Core/Cheat/CheatCommands/` 폴더에 클래스를 생성합니다.

**중요: 클래스명은 CSV의 ID와 동일해야 합니다!**

```csharp
#if UNITY_EDITOR
using Core.Utilities;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 골드 추가 치트
    /// 사용법: AddGold [amount]
    /// </summary>
    public class AddGold : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                GameLogger.Log("[Cheat] 사용법: AddGold [amount]");
                return;
            }

            if (int.TryParse(args[0], out int amount))
            {
                // 실제 골드 추가 로직
                GameContext.Instance.PlayerData.Gold += amount;
                GameLogger.Log($"[Cheat] 골드 {amount} 추가됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 수량: {args[0]}");
            }
        }
    }
}
#endif
```

### 3. 치트 실행

1. 게임 실행 중 `` ` `` (백틱) 키를 눌러 콘솔 열기
2. 치트 명령어 입력 (Tab으로 자동완성 가능)
3. Enter로 실행

```
예시 입력:
> AddGold 1000
> AddItem "Legendary Sword" 5
> SetLevel 99
> GodMode
```

---

## 실전 예제

### 파라미터가 없는 치트

```csharp
#if UNITY_EDITOR
using Core.Utilities;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 무적 모드 토글 치트
    /// 사용법: GodMode
    /// </summary>
    public class GodMode : ICheat
    {
        // static으로 상태 유지
        private static bool isEnabled = false;

        public void Execute(string[] args)
        {
            isEnabled = !isEnabled;

            // 무적 모드 적용
            GameContext.Instance.PlayerData.IsInvincible = isEnabled;
            GameLogger.Log($"[Cheat] 무적 모드: {(isEnabled ? "활성화" : "비활성화")}");
        }
    }
}
#endif
```

### 단일 파라미터 치트

```csharp
#if UNITY_EDITOR
using Core.Utilities;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 레벨 설정 치트
    /// 사용법: SetLevel [level]
    /// </summary>
    public class SetLevel : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                GameLogger.Log("[Cheat] 사용법: SetLevel [level]");
                return;
            }

            if (int.TryParse(args[0], out int level))
            {
                // 레벨 범위 검증
                level = Mathf.Clamp(level, 1, 100);

                GameContext.Instance.PlayerData.Level = level;
                GameLogger.Log($"[Cheat] 레벨이 {level}로 설정됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 레벨: {args[0]}");
            }
        }
    }
}
#endif
```

### 다중 파라미터 치트

```csharp
#if UNITY_EDITOR
using Core.Utilities;
using UnityEngine;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 아이템 추가 치트
    /// 사용법: AddItem [itemId] [count]
    /// 예: AddItem "Legendary Sword" 100
    /// </summary>
    public class AddItem : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 2)
            {
                GameLogger.Log("[Cheat] 사용법: AddItem [itemId] [count]");
                return;
            }

            string itemId = args[0];

            if (int.TryParse(args[1], out int count))
            {
                // 아이템 추가 로직
                var inventory = GameContext.Instance.Inventory;
                inventory.AddItem(itemId, count);

                GameLogger.Log($"[Cheat] 아이템 '{itemId}' {count}개 추가됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 수량: {args[1]}");
            }
        }
    }
}
#endif
```

### 좌표 입력 치트

```csharp
#if UNITY_EDITOR
using Core.Utilities;
using UnityEngine;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 텔레포트 치트
    /// 사용법: Teleport [x] [y] [z]
    /// </summary>
    public class Teleport : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                GameLogger.Log("[Cheat] 사용법: Teleport [x] [y] [z]");
                return;
            }

            bool parseSuccess = true;
            parseSuccess &= float.TryParse(args[0], out float x);
            parseSuccess &= float.TryParse(args[1], out float y);
            parseSuccess &= float.TryParse(args[2], out float z);

            if (parseSuccess)
            {
                Vector3 position = new Vector3(x, y, z);

                // 플레이어 텔레포트
                var player = GameContext.Instance.Player;
                if (player != null)
                {
                    player.transform.position = position;
                }

                GameLogger.Log($"[Cheat] 위치 ({x}, {y}, {z})로 텔레포트됨");
            }
            else
            {
                GameLogger.LogWarning("[Cheat] 잘못된 좌표 형식입니다.");
            }
        }
    }
}
#endif
```

### 적 소환 치트

```csharp
#if UNITY_EDITOR
using Core.Utilities;
using UnityEngine;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 적 소환 치트
    /// 사용법: SpawnEnemy [enemyId] [count] [level]
    /// </summary>
    public class SpawnEnemy : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                GameLogger.Log("[Cheat] 사용법: SpawnEnemy [enemyId] [count] [level]");
                return;
            }

            string enemyId = args[0];

            bool parseSuccess = true;
            parseSuccess &= int.TryParse(args[1], out int count);
            parseSuccess &= int.TryParse(args[2], out int level);

            if (parseSuccess)
            {
                // 적 소환 로직
                var spawner = EnemySpawner.Instance;
                if (spawner != null)
                {
                    for (int i = 0; i < count; i++)
                    {
                        spawner.SpawnEnemy(enemyId, level);
                    }
                }

                GameLogger.Log($"[Cheat] 적 '{enemyId}' Lv.{level} {count}마리 소환됨");
            }
            else
            {
                GameLogger.LogWarning("[Cheat] 잘못된 매개변수 형식입니다.");
            }
        }
    }
}
#endif
```

---

## 고급 기능

### 코드에서 치트 실행

```csharp
#if UNITY_EDITOR
// 프로그래밍 방식으로 치트 실행
CheatManager.Instance.ExecuteCheat("AddGold 1000");
CheatManager.Instance.ExecuteCheat("AddItem \"Legendary Sword\" 5");
#endif
```

### 치트 데이터 조회

```csharp
#if UNITY_EDITOR
// 모든 치트 목록 가져오기
var allCheats = CheatManager.Instance.GetAllCheatData();

// 특정 치트 데이터 가져오기
var cheatData = CheatManager.Instance.GetCheatData("AddGold");
if (cheatData != null)
{
    Debug.Log($"ID: {cheatData.ID}");
    Debug.Log($"Description: {cheatData.Description}");
    Debug.Log($"Usage: {cheatData.GetUsage()}");
}

// 검색어와 매칭되는 치트 목록
var matchingCheats = CheatManager.Instance.GetMatchingCheats("Add");
#endif
```

### 치트 구현 여부 확인

```csharp
#if UNITY_EDITOR
// CSV에 정의되었지만 구현되지 않은 치트 확인
var allCheats = CheatManager.Instance.GetAllCheatData();

for (int i = 0; i < allCheats.Count; i++)
{
    var cheat = allCheats[i];
    bool hasImpl = CheatManager.Instance.HasCheatType(cheat.ID);

    if (!hasImpl)
    {
        Debug.LogWarning($"미구현 치트: {cheat.ID}");
    }
}
#endif
```

### 치트 데이터 리로드

```csharp
#if UNITY_EDITOR
// CSV 수정 후 런타임에 다시 로드
CheatManager.Instance.ReloadCheatData();
#endif
```

---

## 주의사항

### #if UNITY_EDITOR 필수

모든 치트 관련 코드는 `#if UNITY_EDITOR` 전처리기로 감싸야 합니다.

```csharp
// 나쁜 예: 전처리기 없음
public class MyCheat : ICheat
{
    // 빌드에 포함됨!
}

// 좋은 예: 전처리기 사용
#if UNITY_EDITOR
public class MyCheat : ICheat
{
    // Editor에서만 컴파일됨
}
#endif
```

### 클래스명 = CSV ID

ICheat 구현 클래스명은 CSV의 ID와 **정확히 일치**해야 합니다.

```csharp
// CSV: ID = "AddGold"

// 나쁜 예: 이름 불일치
public class AddGoldCheat : ICheat { }  // 작동 안 함!
public class addgold : ICheat { }        // 대소문자 다름!

// 좋은 예: 정확히 일치
public class AddGold : ICheat { }
```

### 네임스페이스

치트 명령어 클래스는 어떤 네임스페이스에 있어도 됩니다. CheatManager가 모든 어셈블리를 검색합니다.

```csharp
// 모두 정상 작동
namespace Core.Cheat.Commands { public class AddGold : ICheat { } }
namespace MyGame.Cheats { public class AddGold : ICheat { } }
public class AddGold : ICheat { }  // 네임스페이스 없음
```

### 파라미터 파싱

인자는 항상 `string[]`로 전달됩니다. 적절한 타입 변환이 필요합니다.

```csharp
// 나쁜 예: 예외 발생 가능
public void Execute(string[] args)
{
    int value = int.Parse(args[0]);  // 변환 실패 시 예외!
}

// 좋은 예: TryParse 사용
public void Execute(string[] args)
{
    if (int.TryParse(args[0], out int value))
    {
        // 성공
    }
    else
    {
        GameLogger.LogWarning("잘못된 형식");
    }
}
```

### 공백 포함 문자열

공백이 포함된 문자열은 큰따옴표로 감싸야 합니다.

```
// 나쁜 예
> AddItem Legendary Sword 5     // "Legendary", "Sword", "5"로 분리됨

// 좋은 예
> AddItem "Legendary Sword" 5   // "Legendary Sword", "5"로 분리됨
```

### 인자 개수 검증

항상 인자 개수를 먼저 확인하세요.

```csharp
// 나쁜 예: IndexOutOfRangeException 발생 가능
public void Execute(string[] args)
{
    string itemId = args[0];
    int count = int.Parse(args[1]);
}

// 좋은 예: 인자 개수 검증
public void Execute(string[] args)
{
    if (args.Length < 2)
    {
        GameLogger.Log("[Cheat] 사용법: AddItem [itemId] [count]");
        return;
    }

    string itemId = args[0];
    if (int.TryParse(args[1], out int count))
    {
        // 로직 실행
    }
}
```

---

## FAQ

### Q1. 치트 콘솔이 열리지 않습니다.

A. 다음을 확인하세요:
1. Editor에서 실행 중인지 (빌드에서는 작동 안 함)
2. InputManager가 초기화되었는지
3. EditorCheatConsole 컴포넌트가 씬에 존재하는지

### Q2. 치트가 실행되지 않습니다.

A. 다음을 확인하세요:
1. CSV에 치트가 정의되어 있는지
2. ICheat 구현 클래스가 있는지
3. **클래스명이 CSV ID와 정확히 일치하는지**
4. 클래스가 `#if UNITY_EDITOR`로 감싸져 있는지

### Q3. 자동완성 목록에 [미구현]이 표시됩니다.

A. CSV에는 정의되어 있지만 ICheat 구현 클래스가 없는 경우입니다. 구현 클래스를 추가하세요.

### Q4. 새로 추가한 치트가 목록에 없습니다.

A. 다음을 시도하세요:
1. CSVManager의 데이터를 다시 로드
2. 치트 콘솔을 닫았다가 다시 열기
3. `CheatManager.Instance.ReloadCheatData()` 호출

### Q5. 공백이 포함된 문자열을 전달하려면?

A. 큰따옴표로 감싸세요.

```
> AddItem "Fire Sword" 10
> SpawnEnemy "Boss Dragon" 1 50
```

### Q6. 치트 실행 로그를 확인하려면?

A. Console 창에서 `[Cheat]` 또는 `[CheatManager]`로 필터링하세요.

### Q7. 빌드에서 치트 시스템이 포함되나요?

A. 아니요, 모든 치트 코드가 `#if UNITY_EDITOR`로 감싸져 있어 빌드에 포함되지 않습니다.

### Q8. 치트 명령어 히스토리는 저장되나요?

A. 현재 세션 내에서만 유지됩니다. 게임을 종료하면 히스토리가 사라집니다.

---

## 요약

**치트 시스템 사용 3단계:**

1. **CSV에 치트 정의**
2. **ICheat 구현 클래스 작성**
3. **게임에서 `` ` `` 키로 콘솔 열고 실행**

```csharp
// 1. CSV 정의 (CheatData.csv)
// ID,Description,Parameters
// AddGold,골드를 추가합니다.,amount:int

// 2. 구현 클래스 작성
#if UNITY_EDITOR
public class AddGold : ICheat
{
    public void Execute(string[] args)
    {
        if (args.Length < 1) return;

        if (int.TryParse(args[0], out int amount))
        {
            // 골드 추가 로직
            GameLogger.Log($"[Cheat] 골드 {amount} 추가됨");
        }
    }
}
#endif

// 3. 게임에서 실행
// > AddGold 1000
```

**핵심 원칙:**
- 모든 치트 코드는 `#if UNITY_EDITOR` 필수
- 클래스명 = CSV ID (정확히 일치)
- 인자 개수 검증 필수
- TryParse로 안전한 타입 변환

**파일 구조:**
```
Assets/
├── Data/CSV/
│   └── CheatData.csv              # 치트 정의 데이터
├── Scripts/
│   ├── Core/Cheat/
│   │   ├── CheatManager.cs        # 치트 관리자
│   │   ├── EditorCheatConsole.cs  # 입력 UI
│   │   ├── CheatInputParser.cs    # 입력 파서
│   │   ├── CheatExtensions.cs     # 확장 메서드
│   │   ├── ICheat.cs              # 인터페이스
│   │   └── CheatCommands/
│   │       └── ExampleCheats.cs   # 예시 치트
│   └── Data/Generated/
│       └── CheatData.cs           # 자동 생성된 데이터 클래스
```

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/Cheat/`
- CSV 데이터: `Assets/Data/CSV/CheatData.csv`
- 예시 치트: `Assets/Scripts/Core/Cheat/CheatCommands/ExampleCheats.cs`
