# Common Scripts

---

## 📁 구조

```
Scripts/
├── Core/              # 핵심 시스템
│   ├── Addressable/   # 리소스 로딩 (참조 카운팅, 중복 방지)
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
