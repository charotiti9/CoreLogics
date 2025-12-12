# InputManager 사용 가이드

## 개요

InputManager는 게임의 모든 입력을 중앙에서 관리하는 시스템입니다. `.inputactions` 파일을 수정하면 자동으로 코드가 생성됩니다.

**장점:**
- ✅ 코드 자동 생성 (수동 작업 최소화)
- ✅ 키보드/마우스, 게임패드, 터치 등 모든 입력 지원
- ✅ 이벤트 기반 구독 시스템
- ✅ 타입 안전성 보장 (IntelliSense 지원)
- ✅ Action Map별 입력 제어 가능
- ✅ 자동 재생성 (파일 수정 시)

---

## 초기 설정 (최초 1회)

### 1. InputActions 파일 설정

1. `Assets/Input/InputSystem_Actions.inputactions` 파일 선택
2. Inspector에서 다음 설정:
   - ✅ **Generate C# Class** 체크
   - **Class Name**: `InputSystem_Actions`
   - **Namespace**: (비워두기)
3. **Apply** 버튼 클릭
4. `InputSystem_Actions.cs` 파일이 자동 생성됨

### 2. InputManager 코드 생성

Unity 에디터 메뉴:
```
Tools > Input > Generate Input Manager Code
```

실행하면:
- `InputManager.Generated.cs` 파일 생성
- 콘솔에 `[InputManager] 코드 자동 생성 완료` 로그 출력

### 3. 자동 재생성 확인

이후 `.inputactions` 파일을 수정하면:
- **자동으로 코드 재생성**
- 수동으로 메뉴 실행 불필요!

---

## 기본 사용법

### 1. 이벤트 구독 방식 (권장)

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void OnEnable()
    {
        // 이벤트 구독
        InputManager.Instance.OnMove += HandleMove;
        InputManager.Instance.OnJumpStarted += HandleJump;
        InputManager.Instance.OnAttackPerformed += HandleAttack;
    }

    private void OnDisable()
    {
        // 반드시 구독 해제!
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMove -= HandleMove;
            InputManager.Instance.OnJumpStarted -= HandleJump;
            InputManager.Instance.OnAttackPerformed -= HandleAttack;
        }
    }

    private void HandleMove(Vector2 input)
    {
        // 이동 처리
        if (input != Vector2.zero)
        {
            transform.Translate(new Vector3(input.x, 0, input.y) * Time.deltaTime * 5f);
        }
    }

    private void HandleJump()
    {
        // 점프 처리
        Debug.Log("Jump!");
    }

    private void HandleAttack()
    {
        // 공격 처리
        Debug.Log("Attack!");
    }
}
```

**⚠️ OnDisable에서 반드시 구독 해제하세요! (메모리 누수 방지)**

### 2. 입력 값 직접 조회 방식

```csharp
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        // 실시간 입력 값 읽기
        Vector2 moveInput = InputManager.Instance.GetMoveInput();

        if (moveInput != Vector2.zero)
        {
            Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
            transform.Translate(movement * moveSpeed * Time.deltaTime);
        }

        // 버튼 상태 확인
        if (InputManager.Instance.IsJumpPressed())
        {
            Jump();
        }

        if (InputManager.Instance.IsSprintPressed())
        {
            moveSpeed = 10f;
        }
        else
        {
            moveSpeed = 5f;
        }
    }

    private void Jump()
    {
        Debug.Log("Jumping!");
    }
}
```

---

## 입력 타입별 사용법

### Vector2 타입 (Move, Look 등)

**이벤트**:
- `On{ActionName}` - 입력 값 변경 시 호출

**Get 메서드**:
- `Get{ActionName}Input()` - 현재 입력 값 반환

```csharp
// 이벤트 방식
InputManager.Instance.OnMove += (input) =>
{
    Debug.Log($"Move: {input}");
};

// Get 방식
Vector2 moveInput = InputManager.Instance.GetMoveInput();
Vector2 lookInput = InputManager.Instance.GetLookInput();
```

### Button 타입 (Jump, Attack 등)

**이벤트**:
- `On{ActionName}Started` - 버튼 막 눌림
- `On{ActionName}Performed` - 버튼 눌림 유지
- `On{ActionName}Canceled` - 버튼 떼어짐

**Get 메서드**:
- `Is{ActionName}Pressed()` - 현재 눌림 상태

```csharp
// 이벤트 방식
InputManager.Instance.OnJumpStarted += () =>
{
    Debug.Log("Jump Started!");
};

InputManager.Instance.OnJumpCanceled += () =>
{
    Debug.Log("Jump Canceled!");
};

// Get 방식
if (InputManager.Instance.IsJumpPressed())
{
    Debug.Log("Jump is pressed!");
}
```

---

## 예제 모음

### 플레이어 이동 및 점프

```csharp
public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private Rigidbody rb;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        InputManager.Instance.OnMove += HandleMove;
        InputManager.Instance.OnJumpStarted += HandleJump;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMove -= HandleMove;
            InputManager.Instance.OnJumpStarted -= HandleJump;
        }
    }

    private void HandleMove(Vector2 input)
    {
        Vector3 movement = new Vector3(input.x, 0, input.y) * moveSpeed;
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
    }

    private void HandleJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
```

### 카메라 회전

```csharp
public class CameraController : MonoBehaviour
{
    public Transform playerTransform;
    public float sensitivity = 2f;
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void OnEnable()
    {
        InputManager.Instance.OnLook += HandleLook;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnLook -= HandleLook;
        }
    }

    private void HandleLook(Vector2 input)
    {
        rotationX += input.y * sensitivity;
        rotationY += input.x * sensitivity;

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.rotation = Quaternion.Euler(-rotationX, rotationY, 0);
    }
}
```

### 공격 및 콤보

```csharp
public class PlayerCombat : MonoBehaviour
{
    private int comboCount = 0;
    private float lastAttackTime = 0f;
    private float comboResetTime = 1f;

    private void OnEnable()
    {
        InputManager.Instance.OnAttackStarted += HandleAttack;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnAttackStarted -= HandleAttack;
        }
    }

    private void HandleAttack()
    {
        // 콤보 리셋 체크
        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboCount = 0;
        }

        comboCount++;
        lastAttackTime = Time.time;

        Debug.Log($"Attack! Combo: {comboCount}");

        // 콤보별 공격 실행
        switch (comboCount)
        {
            case 1:
                PerformAttack1();
                break;
            case 2:
                PerformAttack2();
                break;
            case 3:
                PerformAttack3();
                comboCount = 0; // 콤보 리셋
                break;
        }
    }

    private void PerformAttack1() { Debug.Log("Quick Slash!"); }
    private void PerformAttack2() { Debug.Log("Heavy Slash!"); }
    private void PerformAttack3() { Debug.Log("Finish Move!"); }
}
```

---

## 입력 제어

### UI 팝업 시 입력 차단

```csharp
using Common.UI;

public class PauseMenuUI : UIBase
{
    public override void OnShow()
    {
        base.OnShow();

        // 플레이어 입력 비활성화, UI 입력 활성화
        InputManager.Instance.DisablePlayerInput();
        InputManager.Instance.EnableUIInput();

    }

    public override void OnHide()
    {
        base.OnHide();

        // 플레이어 입력 재활성화, UI 입력 비활성화
        InputManager.Instance.EnablePlayerInput();
        InputManager.Instance.DisableUIInput();

    }
}
```

### 모든 입력 비활성화

```csharp
public class Cutscene : MonoBehaviour
{
    private void OnEnable()
    {
        // 컷씬 시작 시 모든 입력 비활성화
        InputManager.Instance.DisableAllInput();
    }

    private void OnDisable()
    {
        // 컷씬 종료 시 플레이어 입력 재활성화
        InputManager.Instance.EnablePlayerInput();
    }
}
```

---

## 새로운 액션 추가하기

### 워크플로우

**예시: "Dash" 액션 추가**

1. **Input Actions 파일 열기**
   - `Assets/Input/InputSystem_Actions.inputactions` 더블클릭

2. **액션 추가**
   - Player Action Map 선택
   - "+" 버튼으로 새 액션 추가
   - 이름: "Dash"
   - Action Type: "Button"

3. **키 바인딩 설정**
   - Bindings에서 "+" 버튼
   - Keyboard: Left Shift 선택
   - Gamepad: Left Stick Press 선택

4. **저장**
   - "Save Asset" 클릭 또는 Ctrl+S

5. **자동 재생성 확인**
   - 콘솔에 `[InputManager] InputActions 파일 변경 감지` 로그 출력
   - `[InputManager] 코드 자동 생성 완료` 로그 확인

6. **바로 사용!**

```csharp
public class PlayerDash : MonoBehaviour
{
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    private bool isDashing = false;

    private void OnEnable()
    {
        // 자동 생성된 이벤트 구독
        InputManager.Instance.OnDashStarted += StartDash;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnDashStarted -= StartDash;
        }
    }

    private async void StartDash()
    {
        if (isDashing) return;

        isDashing = true;

        Vector2 moveInput = InputManager.Instance.GetMoveInput();
        Vector3 dashDirection = new Vector3(moveInput.x, 0, moveInput.y);

        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;

        // 대시 실행
        float timer = 0f;
        while (timer < dashDuration)
        {
            transform.Translate(dashDirection * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            await System.Threading.Tasks.Task.Yield();
        }

        isDashing = false;
    }
}
```

---

## 주의사항

### ⚠️ 이벤트 구독 해제 필수

```csharp
// ❌ 나쁜 예 - 구독 해제 안 함
public class BadExample : MonoBehaviour
{
    private void OnEnable()
    {
        InputManager.Instance.OnMove += HandleMove;
    }
    // OnDisable에서 구독 해제 안 함 → 메모리 누수!
}

// ✅ 좋은 예 - 구독 해제
public class GoodExample : MonoBehaviour
{
    private void OnEnable()
    {
        InputManager.Instance.OnMove += HandleMove;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnMove -= HandleMove;
        }
    }

    private void HandleMove(Vector2 input) { }
}
```

### ⚠️ InputManager.Generated.cs 직접 수정 금지

`InputManager.Generated.cs`는 자동 생성 파일입니다.
- ❌ 직접 수정하면 재생성 시 덮어씌워집니다.
- ✅ 추가 기능이 필요하면 `InputManager.cs`를 수정하세요.

### ⚠️ .inputactions 파일 저장 확인

- 액션 추가 후 반드시 저장 (Ctrl+S)
- 저장하지 않으면 자동 재생성 안 됨

---

## 트러블슈팅

### "InputSystem_Actions를 찾을 수 없습니다" 오류

**원인**: Unity Input System의 C# 클래스가 생성되지 않음

**해결**:
1. `InputSystem_Actions.inputactions` 파일 선택
2. Inspector에서 "Generate C# Class" 체크
3. Apply 버튼 클릭
4. 컴파일 완료 대기

### "BindAllActions를 찾을 수 없습니다" 오류

**원인**: `InputManager.Generated.cs` 파일이 생성되지 않음

**해결**:
1. `Tools > Input > Generate Input Manager Code` 메뉴 실행
2. `InputManager.Generated.cs` 파일 생성 확인
3. 컴파일 완료 대기

### 자동 재생성이 작동하지 않음

**디버깅**:
1. `.inputactions` 파일 저장 확인 (Ctrl+S)
2. 콘솔에서 `[InputManager]` 로그 확인
3. 오류 메시지 확인

**해결**:
- 수동으로 `Tools > Input > Generate Input Manager Code` 실행
- Unity 재시작

---

## 요약

**InputManager 사용 3단계:**

1. **초기 설정** (최초 1회만)
   - InputActions 파일에서 "Generate C# Class" 활성화
   - `Tools > Input > Generate Input Manager Code` 실행

2. **이벤트 구독**
   ```csharp
   InputManager.Instance.OnMove += HandleMove;
   ```

3. **구독 해제** (OnDisable에서 필수!)
   ```csharp
   InputManager.Instance.OnMove -= HandleMove;
   ```

**새 액션 추가:**
1. `.inputactions` 파일에서 액션 추가
2. 파일 저장 (Ctrl+S)
3. **자동으로 코드 재생성**
4. 바로 사용!

**추가 정보:**
- 기술 문서: `Assets/Scripts/Core/Input/README.md`
- 소스 코드: `Assets/Scripts/Core/Input/`
- 자동 생성 코드: `Assets/Scripts/Core/Input/InputManager.Generated.cs`
