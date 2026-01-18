# Common Scripts

---

## 📁 구조

```
Scripts/
├── Core/              # 핵심 시스템
│   ├── Addressable/   # 리소스 로딩 (참조 카운팅, 중복 방지)
│   ├── Cheat/         # 치트 콘솔 (Editor 전용, 자동완성)
│   ├── CSV/           # CSV 데이터 관리 (자동 파싱, 참조 해결)
│   ├── Game/          # 게임 부트스트랩 및 상태 관리
│   ├── GameFlow/      # 중앙집중식 Update 관리
│   ├── Input/         # 입력 관리 (자동 코드 생성, 게임패드 지원)
│   ├── Pool/          # 오브젝트 풀링 (Addressable 통합)
│   ├── Singleton/     # 싱글톤 패턴
│   ├── StateMachine/  # 상태 머신
│   └── Utilities/     # 유틸리티
└── Common/            # 공통 기능
    ├── Audio/         # 오디오 관리 (BGM, SFX, Voice)
    └── UI/            # UI 관리 (레이어, 스택, Dim)
```

---

## ⚙️ 사전 설정

### 필수 패키지

**1. Addressable Asset System**
- 설치: Package Manager → Addressables
- 설정: Window → Addressables → Groups → Create Settings
- 용도: 모든 리소스 로딩 (Resources 폴더 사용 금지)

**2. DoTween**
- 설치: Asset Store → DOTween 임포트
- 설정: Tools → DOTween Utility Panel → Setup
- 용도: UI 애니메이션

**3. UniTask**
- 설치: Package Manager → Add from git URL
  - `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- 용도: 비동기 처리 (코루틴 대체, GC 0)

**4. Input System**
- 설치: Package Manager → Input System
- 용도: 통합 입력 관리 (키보드, 마우스, 게임패드, 터치 등)

### 씬 설정

게임 시작 씬에 배치:
- `[GameBootstrap]` - GameBootstrap 컴포넌트
- `[GameFlowManager]` - GameFlowManager 컴포넌트
- `[UIManager]` - UIManager 컴포넌트
- `[AudioManager]` - AudioManager 컴포넌트

### Input System 초기 설정 (최초 1회)

**1. InputActions 파일 설정**
- `Assets/Input/InputSystem_Actions.inputactions` 파일 선택
- Inspector에서:
  - ✅ **Generate C# Class** 체크
  - Class Name: `InputSystem_Actions`
  - Namespace: (비워두기)
- **Apply** 버튼 클릭

**2. InputManager 코드 생성**
- Unity 메뉴: `Tools > Input > Generate Input Manager Code`
- 콘솔에 생성 완료 로그 확인

**3. 자동 재생성 확인**
- `.inputactions` 파일 수정 시 자동으로 코드 재생성됨
- 이후 수동 생성 불필요

---

## 🎮 Input System

### 핵심 기능

- ✅ 코드 자동 생성 (.inputactions 파일 수정 시)
- ✅ 키보드/마우스, 게임패드, 터치 등 모든 입력 지원
- ✅ 이벤트 기반 구독 시스템
- ✅ 타입 안전성 보장 (IntelliSense 지원)
- ✅ Action Map별 입력 제어

### 기본 사용법

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void OnEnable()
    {
        // 이벤트 구독
        InputManager.Instance.OnMove += HandleMove;
        InputManager.Instance.OnJumpStarted += HandleJump;
    }

    private void OnDisable()
    {
        // 구독 해제
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMove -= HandleMove;
            InputManager.Instance.OnJumpStarted -= HandleJump;
        }
    }

    private void HandleMove(Vector2 input)
    {
        // 이동 처리
        transform.Translate(new Vector3(input.x, 0, input.y) * Time.deltaTime * 5f);
    }

    private void HandleJump()
    {
        // 점프 처리
    }
}
```

### 현재 입력 값 조회

```csharp
private void Update()
{
    // 실시간 입력 값 읽기
    Vector2 moveInput = InputManager.Instance.GetMoveInput();
    bool isJumping = InputManager.Instance.IsJumpPressed();
}
```

### 입력 제어

```csharp
// 플레이어 입력 비활성화 (예: UI 팝업 시)
InputManager.Instance.DisablePlayerInput();
InputManager.Instance.EnableUIInput();

// 플레이어 입력 재활성화
InputManager.Instance.EnablePlayerInput();
InputManager.Instance.DisableUIInput();
```

### 새로운 액션 추가하기

1. `Assets/Input/InputSystem_Actions.inputactions` 파일 열기
2. 원하는 Action Map에서 "+" 버튼으로 액션 추가
3. 액션 설정 (이름, 타입, 키 바인딩)
4. 파일 저장 (Ctrl+S)
5. **자동으로 코드 재생성** (콘솔 확인)
6. 바로 사용 가능!

```csharp
// 자동 생성된 이벤트 사용
InputManager.Instance.OnDashStarted += HandleDash;
```

**자세한 사용법**: `Assets/Docs/InputManager_Guide.md` 참조

---

## 🎯 Cheat System (Editor 전용)

개발 및 테스트를 위한 치트 콘솔 시스템입니다. **Editor에서만 동작**하며, 빌드에는 포함되지 않습니다.

### 핵심 기능

- ✅ [`] 키로 치트 콘솔 열기
- ✅ 자동완성 기능 (Tab 키)
- ✅ 히스토리 탐색 (Ctrl+↑, Ctrl+↓)
- ✅ 파라미터 가이드 표시
- ✅ 리플렉션 기반 치트 자동 등록
- ✅ CSV 기반 치트 정보 관리

### 씬 설정

치트 콘솔을 사용하려면 씬에 `EditorCheatConsole` 컴포넌트를 추가합니다:
- 빈 GameObject 생성 → `EditorCheatConsole` 컴포넌트 추가
- 또는 `[EditorCheatConsole]` 프리팹 배치

### 치트 콘솔 사용법

```
[`] 키     : 콘솔 열기
ESC        : 콘솔 닫기
Tab        : 선택된 치트로 자동완성
↑/↓        : 자동완성 목록 탐색
Ctrl+↑/↓   : 명령어 히스토리 탐색
Enter      : 명령어 실행
```

### 새로운 치트 추가하기

**1. ICheat 인터페이스 구현**

`Assets/Scripts/Core/Cheat/CheatCommands/` 폴더에 치트 클래스를 생성합니다.

```csharp
#if UNITY_EDITOR
using Core.Utilities;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 체력 설정 치트
    /// 사용법: SetHealth [amount]
    /// </summary>
    public class SetHealth : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                GameLogger.Log("[Cheat] 사용법: SetHealth [amount]");
                return;
            }

            if (int.TryParse(args[0], out int amount))
            {
                // 실제 체력 설정 로직
                GameLogger.Log($"[Cheat] 체력이 {amount}로 설정됨");
            }
        }
    }
}
#endif
```

**2. CSV에 치트 정보 등록**

`Assets/Data/CSV/CheatData.csv`에 치트 정보를 추가합니다:

```csv
ID,Description,Parameters
SetHealth,플레이어 체력을 설정합니다,amount:int
```

**Parameters 형식**: `파라미터명:타입|파라미터명:타입|...`

예시:
- `amount:int` - 정수 파라미터 1개
- `itemId:string|count:int` - 문자열과 정수 파라미터 2개
- `x:float|y:float|z:float` - 실수 파라미터 3개

### 제공되는 예시 치트

| 치트 ID | 설명 | 사용법 |
|---------|------|--------|
| AddGold | 골드 추가 | `AddGold [amount]` |
| AddItem | 아이템 추가 | `AddItem [itemId] [count]` |
| SetLevel | 레벨 설정 | `SetLevel [level]` |
| GodMode | 무적 모드 토글 | `GodMode` |
| ClearStage | 스테이지 클리어 | `ClearStage` |
| Teleport | 텔레포트 | `Teleport [x] [y] [z]` |
| SpawnEnemy | 적 소환 | `SpawnEnemy [enemyId] [count] [level]` |

### 파일 구조

```
Core/Cheat/
├── ICheat.cs              # 치트 인터페이스
├── CheatManager.cs        # 치트 시스템 관리자
├── CheatInputParser.cs    # 입력 문자열 파싱
├── CheatExtensions.cs     # CheatData 확장 메서드
├── EditorCheatConsole.cs  # 치트 콘솔 UI
└── CheatCommands/         # 치트 구현 클래스
    └── ExampleCheats.cs   # 예시 치트들
```

### 주의사항

- 치트 클래스명은 반드시 `CheatData.csv`의 ID와 **동일**해야 합니다
- 모든 치트 코드는 `#if UNITY_EDITOR`로 감싸야 합니다
- 큰따옴표로 감싼 문자열은 하나의 파라미터로 처리됩니다
  - 예: `AddItem "Legendary Sword" 100`
