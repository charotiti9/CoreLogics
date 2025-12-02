# StateMachine 사용 가이드

## 개요

StateMachine은 오브젝트의 상태를 체계적으로 관리하는 시스템입니다.

**장점:**
- ✅ 복잡한 상태 로직을 명확하게 분리
- ✅ 상태 간 전환을 쉽게 관리
- ✅ GameFlowManager와 자동 통합
- ✅ 상태 전환 검증 및 이벤트 지원
- ✅ Generic 기반으로 재사용 가능

---

## 핵심 개념

### 1. State Machine이란?

오브젝트가 여러 상태를 가지고, 상태에 따라 다르게 동작하는 패턴입니다.

**기존 방식 (복잡):**
```csharp
public class Enemy : MonoBehaviour
{
    enum State { Idle, Patrol, Chase, Attack }
    private State currentState;

    void Update()
    {
        // 모든 상태 로직이 한 곳에...
        if (currentState == State.Idle)
        {
            // Idle 로직
        }
        else if (currentState == State.Patrol)
        {
            // Patrol 로직
        }
        else if (currentState == State.Chase)
        {
            // Chase 로직
        }
        // ... 점점 복잡해짐
    }
}
```

**StateMachine 방식 (명확):**
```csharp
// 각 상태가 독립적인 클래스로 분리
public class IdleState : IState<EnemyContext> { }
public class PatrolState : IState<EnemyContext> { }
public class ChaseState : IState<EnemyContext> { }

// 상태 전환만 관리
stateMachine.ChangeState(new ChaseState());
```

### 2. 핵심 구성 요소

**Context (컨텍스트):**
- 모든 상태가 공유하는 데이터
- 예: 적의 체력, 이동 속도, Transform 등

**State (상태):**
- 각 상태의 로직을 담은 클래스
- Enter, Update, Exit 메서드 구현

**StateMachine (상태 머신):**
- 상태 전환을 관리하는 매니저
- GameFlowManager와 자동 통합

### 3. 상태 생명주기

```
[이전 상태] Exit → [새 상태] Enter → [새 상태] Update (매 프레임)
```

1. **Enter**: 상태에 진입할 때 한 번 호출 (초기화)
2. **Update**: 상태가 활성화되어 있는 동안 매 프레임 호출
3. **Exit**: 상태에서 나갈 때 한 번 호출 (정리)

---

## 기본 사용법

### 1. Context 클래스 정의

모든 상태가 공유할 데이터를 담는 클래스입니다.

```csharp
public class PlayerContext
{
    // 공유 데이터
    public Transform Transform;
    public Rigidbody Rigidbody;
    public float MoveSpeed;
    public int Health;

    // 생성자
    public PlayerContext(Transform transform, Rigidbody rigidbody)
    {
        Transform = transform;
        Rigidbody = rigidbody;
        MoveSpeed = 5f;
        Health = 100;
    }
}
```

### 2. 상태 클래스 작성

`IState<TContext>` 인터페이스를 구현합니다.

```csharp
using UnityEngine;

// Idle 상태
public class PlayerIdleState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        Debug.Log("플레이어 대기 상태 진입");
        // 애니메이션 재생 등
    }

    public void Update(PlayerContext context, float deltaTime)
    {
        // 입력 감지
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 외부에서 상태 전환을 처리해야 함 (여기서는 로직만)
        }
    }

    public void Exit(PlayerContext context)
    {
        Debug.Log("플레이어 대기 상태 종료");
    }
}

// Run 상태
public class PlayerRunState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        Debug.Log("플레이어 달리기 상태 진입");
    }

    public void Update(PlayerContext context, float deltaTime)
    {
        // 이동 로직
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        context.Transform.Translate(direction * context.MoveSpeed * deltaTime);
    }

    public void Exit(PlayerContext context)
    {
        Debug.Log("플레이어 달리기 상태 종료");
    }
}
```

### 3. StateMachine 생성 및 사용

```csharp
using UnityEngine;

public class Player : MonoBehaviour
{
    private StateMachine<PlayerContext> stateMachine;
    private PlayerContext context;

    private void Awake()
    {
        // 1. Context 생성
        context = new PlayerContext(transform, GetComponent<Rigidbody>());

        // 2. StateMachine 생성 (GameFlowManager에 자동 등록)
        stateMachine = new StateMachine<PlayerContext>(context);

        // 3. 초기 상태 설정
        stateMachine.ChangeState(new PlayerIdleState());
    }

    private void Update()
    {
        // 상태 전환 로직
        if (stateMachine.CurrentState is PlayerIdleState)
        {
            // Idle 상태에서 이동 키를 누르면 Run으로 전환
            if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
            {
                stateMachine.ChangeState(new PlayerRunState());
            }
        }
        else if (stateMachine.CurrentState is PlayerRunState)
        {
            // Run 상태에서 이동 키를 떼면 Idle로 전환
            if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                stateMachine.ChangeState(new PlayerIdleState());
            }
        }
    }

    private void OnDestroy()
    {
        // 4. 정리 (필수!)
        stateMachine?.Dispose();
    }
}
```

**핵심 포인트:**
- Context는 상태들이 공유하는 데이터
- StateMachine은 자동으로 GameFlowManager에 등록되어 Update 호출됨
- OnDestroy에서 반드시 Dispose() 호출

### 4. 상태 전환 조건을 상태 내부에서 처리

상태 내부에서 전환 조건을 판단하려면 StateMachine 참조가 필요합니다.

```csharp
// Context에 StateMachine 추가
public class PlayerContext
{
    public Transform Transform;
    public Rigidbody Rigidbody;
    public float MoveSpeed;
    public StateMachine<PlayerContext> StateMachine; // 추가

    public PlayerContext(Transform transform, Rigidbody rigidbody)
    {
        Transform = transform;
        Rigidbody = rigidbody;
        MoveSpeed = 5f;
    }
}

// Idle 상태에서 자체적으로 전환
public class PlayerIdleState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        Debug.Log("대기 상태");
    }

    public void Update(PlayerContext context, float deltaTime)
    {
        // 이동 입력 감지 시 Run 상태로 전환
        if (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)
        {
            context.StateMachine.ChangeState(new PlayerRunState());
        }
    }

    public void Exit(PlayerContext context)
    {
    }
}

// Player.cs
public class Player : MonoBehaviour
{
    private void Awake()
    {
        context = new PlayerContext(transform, GetComponent<Rigidbody>());
        stateMachine = new StateMachine<PlayerContext>(context);

        // Context에 StateMachine 참조 전달
        context.StateMachine = stateMachine;

        stateMachine.ChangeState(new PlayerIdleState());
    }
}
```

### 5. 이전 상태로 되돌리기

```csharp
// 공격 상태
public class PlayerAttackState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        Debug.Log("공격!");
    }

    public void Update(PlayerContext context, float deltaTime)
    {
        // 공격 애니메이션이 끝나면 이전 상태로 복귀
        if (IsAttackFinished())
        {
            context.StateMachine.RevertToPreviousState();
        }
    }

    public void Exit(PlayerContext context)
    {
    }

    private bool IsAttackFinished()
    {
        // 공격 애니메이션 종료 체크
        return true;
    }
}
```

### 6. 상태 전환 이벤트

상태가 바뀔 때 이벤트를 받을 수 있습니다.

```csharp
private void Awake()
{
    context = new PlayerContext(transform, GetComponent<Rigidbody>());
    stateMachine = new StateMachine<PlayerContext>(context);
    context.StateMachine = stateMachine;

    // 상태 전환 이벤트 구독
    stateMachine.OnStateChanged += OnStateChanged;

    stateMachine.ChangeState(new PlayerIdleState());
}

private void OnStateChanged(IState<PlayerContext> previousState, IState<PlayerContext> currentState)
{
    Debug.Log($"상태 전환: {previousState?.GetType().Name ?? "null"} → {currentState.GetType().Name}");

    // UI 업데이트, 사운드 재생 등
}

private void OnDestroy()
{
    stateMachine.OnStateChanged -= OnStateChanged;
    stateMachine?.Dispose();
}
```

### 7. 상태 전환 검증

특정 조건에서만 상태 전환을 허용할 수 있습니다.

```csharp
private void Awake()
{
    context = new PlayerContext(transform, GetComponent<Rigidbody>());
    stateMachine = new StateMachine<PlayerContext>(context);
    context.StateMachine = stateMachine;

    // 상태 전환 검증 설정
    stateMachine.TransitionValidator = ValidateTransition;

    stateMachine.ChangeState(new PlayerIdleState());
}

private bool ValidateTransition(IState<PlayerContext> from, IState<PlayerContext> to)
{
    // Dead 상태에서는 다른 상태로 전환 불가
    if (from is PlayerDeadState)
    {
        Debug.LogWarning("죽은 상태에서는 상태 전환 불가!");
        return false;
    }

    // 체력이 0이면 Dead 상태로만 전환 가능
    if (context.Health <= 0 && !(to is PlayerDeadState))
    {
        Debug.LogWarning("체력이 0이면 Dead 상태로만 전환 가능!");
        return false;
    }

    return true;
}
```

---

## 실전 예제

### 적 AI 시스템

```csharp
// Context
public class EnemyContext
{
    public Transform Transform;
    public Transform PlayerTransform;
    public float MoveSpeed = 3f;
    public float ChaseSpeed = 6f;
    public float AttackRange = 2f;
    public float DetectRange = 10f;
    public int Health = 100;
    public StateMachine<EnemyContext> StateMachine;
}

// Idle 상태
public class EnemyIdleState : IState<EnemyContext>
{
    private float idleTime;
    private const float MAX_IDLE_TIME = 2f;

    public void Enter(EnemyContext context)
    {
        Debug.Log("적: 대기 중");
        idleTime = 0f;
    }

    public void Update(EnemyContext context, float deltaTime)
    {
        idleTime += deltaTime;

        // 플레이어 감지
        float distance = Vector3.Distance(context.Transform.position, context.PlayerTransform.position);

        if (distance < context.DetectRange)
        {
            context.StateMachine.ChangeState(new EnemyChaseState());
            return;
        }

        // 일정 시간 후 순찰
        if (idleTime >= MAX_IDLE_TIME)
        {
            context.StateMachine.ChangeState(new EnemyPatrolState());
        }
    }

    public void Exit(EnemyContext context)
    {
    }
}

// Patrol 상태
public class EnemyPatrolState : IState<EnemyContext>
{
    private Vector3 targetPosition;
    private float patrolRadius = 10f;

    public void Enter(EnemyContext context)
    {
        Debug.Log("적: 순찰 시작");
        SetRandomTarget(context);
    }

    public void Update(EnemyContext context, float deltaTime)
    {
        // 플레이어 감지
        float distanceToPlayer = Vector3.Distance(context.Transform.position, context.PlayerTransform.position);

        if (distanceToPlayer < context.DetectRange)
        {
            context.StateMachine.ChangeState(new EnemyChaseState());
            return;
        }

        // 목표 지점으로 이동
        Vector3 direction = (targetPosition - context.Transform.position).normalized;
        context.Transform.position += direction * context.MoveSpeed * deltaTime;

        // 목표 도착 시 새로운 목표 설정
        if (Vector3.Distance(context.Transform.position, targetPosition) < 0.5f)
        {
            SetRandomTarget(context);
        }
    }

    public void Exit(EnemyContext context)
    {
    }

    private void SetRandomTarget(EnemyContext context)
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * patrolRadius;
        targetPosition = context.Transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
}

// Chase 상태
public class EnemyChaseState : IState<EnemyContext>
{
    public void Enter(EnemyContext context)
    {
        Debug.Log("적: 추격 시작!");
    }

    public void Update(EnemyContext context, float deltaTime)
    {
        float distance = Vector3.Distance(context.Transform.position, context.PlayerTransform.position);

        // 공격 범위 내 진입 시 공격 상태로 전환
        if (distance < context.AttackRange)
        {
            context.StateMachine.ChangeState(new EnemyAttackState());
            return;
        }

        // 플레이어 놓침
        if (distance > context.DetectRange * 1.5f)
        {
            context.StateMachine.ChangeState(new EnemyIdleState());
            return;
        }

        // 플레이어 추격
        Vector3 direction = (context.PlayerTransform.position - context.Transform.position).normalized;
        context.Transform.position += direction * context.ChaseSpeed * deltaTime;
        context.Transform.LookAt(context.PlayerTransform);
    }

    public void Exit(EnemyContext context)
    {
    }
}

// Attack 상태
public class EnemyAttackState : IState<EnemyContext>
{
    private float attackCooldown = 0f;
    private const float ATTACK_INTERVAL = 1f;

    public void Enter(EnemyContext context)
    {
        Debug.Log("적: 공격!");
        attackCooldown = 0f;
    }

    public void Update(EnemyContext context, float deltaTime)
    {
        float distance = Vector3.Distance(context.Transform.position, context.PlayerTransform.position);

        // 공격 범위 벗어남
        if (distance > context.AttackRange)
        {
            context.StateMachine.ChangeState(new EnemyChaseState());
            return;
        }

        // 공격
        attackCooldown += deltaTime;
        if (attackCooldown >= ATTACK_INTERVAL)
        {
            Debug.Log("적이 플레이어를 공격!");
            attackCooldown = 0f;
        }

        // 플레이어를 바라봄
        context.Transform.LookAt(context.PlayerTransform);
    }

    public void Exit(EnemyContext context)
    {
    }
}

// Enemy.cs
public class Enemy : MonoBehaviour
{
    private StateMachine<EnemyContext> stateMachine;
    private EnemyContext context;

    public Transform playerTransform;

    private void Awake()
    {
        context = new EnemyContext
        {
            Transform = transform,
            PlayerTransform = playerTransform
        };

        stateMachine = new StateMachine<EnemyContext>(context);
        context.StateMachine = stateMachine;

        // 초기 상태
        stateMachine.ChangeState(new EnemyIdleState());
    }

    public void TakeDamage(int damage)
    {
        context.Health -= damage;

        if (context.Health <= 0)
        {
            // 사망 처리
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}
```

### 게임 상태 관리

```csharp
// Context
public class GameContext
{
    public int Score;
    public int Level;
    public bool IsPaused;
    public StateMachine<GameContext> StateMachine;
}

// Title 상태
public class GameTitleState : IState<GameContext>
{
    public void Enter(GameContext context)
    {
        Debug.Log("타이틀 화면");
        // UI 표시
    }

    public void Update(GameContext context, float deltaTime)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            context.StateMachine.ChangeState(new GamePlayingState());
        }
    }

    public void Exit(GameContext context)
    {
        // UI 숨김
    }
}

// Playing 상태
public class GamePlayingState : IState<GameContext>
{
    public void Enter(GameContext context)
    {
        Debug.Log("게임 시작!");
        context.Score = 0;
        context.Level = 1;
        Time.timeScale = 1f;
    }

    public void Update(GameContext context, float deltaTime)
    {
        // 게임 로직

        // 일시정지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.ChangeState(new GamePausedState());
        }

        // 게임 오버 조건
        if (context.Score < 0)
        {
            context.StateMachine.ChangeState(new GameOverState());
        }
    }

    public void Exit(GameContext context)
    {
    }
}

// Paused 상태
public class GamePausedState : IState<GameContext>
{
    public void Enter(GameContext context)
    {
        Debug.Log("일시정지");
        Time.timeScale = 0f;
        context.IsPaused = true;
        // 일시정지 UI 표시
    }

    public void Update(GameContext context, float deltaTime)
    {
        // 일시정지 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            context.StateMachine.RevertToPreviousState();
        }
    }

    public void Exit(GameContext context)
    {
        Time.timeScale = 1f;
        context.IsPaused = false;
        // 일시정지 UI 숨김
    }
}

// GameOver 상태
public class GameOverState : IState<GameContext>
{
    public void Enter(GameContext context)
    {
        Debug.Log($"게임 오버! 최종 점수: {context.Score}");
        // 게임 오버 UI 표시
    }

    public void Update(GameContext context, float deltaTime)
    {
        // 재시작
        if (Input.GetKeyDown(KeyCode.R))
        {
            context.StateMachine.ChangeState(new GamePlayingState());
        }

        // 타이틀로
        if (Input.GetKeyDown(KeyCode.T))
        {
            context.StateMachine.ChangeState(new GameTitleState());
        }
    }

    public void Exit(GameContext context)
    {
        // 게임 오버 UI 숨김
    }
}

// GameManager.cs
public class GameManager : MonoBehaviour
{
    private StateMachine<GameContext> stateMachine;
    private GameContext context;

    private void Awake()
    {
        context = new GameContext();
        stateMachine = new StateMachine<GameContext>(context);
        context.StateMachine = stateMachine;

        // 타이틀 화면부터 시작
        stateMachine.ChangeState(new GameTitleState());
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }

    public void AddScore(int amount)
    {
        context.Score += amount;
    }
}
```

### 문 열림/닫힘 상태

```csharp
// Context
public class DoorContext
{
    public Transform Transform;
    public Vector3 ClosedPosition;
    public Vector3 OpenedPosition;
    public float MoveSpeed = 2f;
    public StateMachine<DoorContext> StateMachine;
}

// Closed 상태
public class DoorClosedState : IState<DoorContext>
{
    public void Enter(DoorContext context)
    {
        Debug.Log("문 닫힘");
    }

    public void Update(DoorContext context, float deltaTime)
    {
        // 위치 보정 (이미 닫힌 위치에 있도록)
        context.Transform.position = Vector3.Lerp(
            context.Transform.position,
            context.ClosedPosition,
            deltaTime * context.MoveSpeed
        );
    }

    public void Exit(DoorContext context)
    {
    }
}

// Opening 상태
public class DoorOpeningState : IState<DoorContext>
{
    public void Enter(DoorContext context)
    {
        Debug.Log("문 열리는 중...");
    }

    public void Update(DoorContext context, float deltaTime)
    {
        // 문이 열리는 애니메이션
        context.Transform.position = Vector3.MoveTowards(
            context.Transform.position,
            context.OpenedPosition,
            deltaTime * context.MoveSpeed
        );

        // 완전히 열렸으면 Opened 상태로 전환
        if (Vector3.Distance(context.Transform.position, context.OpenedPosition) < 0.01f)
        {
            context.StateMachine.ChangeState(new DoorOpenedState());
        }
    }

    public void Exit(DoorContext context)
    {
    }
}

// Opened 상태
public class DoorOpenedState : IState<DoorContext>
{
    private float openTime = 0f;
    private const float AUTO_CLOSE_TIME = 3f;

    public void Enter(DoorContext context)
    {
        Debug.Log("문 열림");
        openTime = 0f;
    }

    public void Update(DoorContext context, float deltaTime)
    {
        openTime += deltaTime;

        // 일정 시간 후 자동으로 닫힘
        if (openTime >= AUTO_CLOSE_TIME)
        {
            context.StateMachine.ChangeState(new DoorClosingState());
        }
    }

    public void Exit(DoorContext context)
    {
    }
}

// Closing 상태
public class DoorClosingState : IState<DoorContext>
{
    public void Enter(DoorContext context)
    {
        Debug.Log("문 닫히는 중...");
    }

    public void Update(DoorContext context, float deltaTime)
    {
        // 문이 닫히는 애니메이션
        context.Transform.position = Vector3.MoveTowards(
            context.Transform.position,
            context.ClosedPosition,
            deltaTime * context.MoveSpeed
        );

        // 완전히 닫혔으면 Closed 상태로 전환
        if (Vector3.Distance(context.Transform.position, context.ClosedPosition) < 0.01f)
        {
            context.StateMachine.ChangeState(new DoorClosedState());
        }
    }

    public void Exit(DoorContext context)
    {
    }
}

// Door.cs
public class Door : MonoBehaviour
{
    private StateMachine<DoorContext> stateMachine;
    private DoorContext context;

    public Vector3 openOffset = new Vector3(0, 3, 0);

    private void Awake()
    {
        context = new DoorContext
        {
            Transform = transform,
            ClosedPosition = transform.position,
            OpenedPosition = transform.position + openOffset
        };

        stateMachine = new StateMachine<DoorContext>(context);
        context.StateMachine = stateMachine;

        // 초기 상태: 닫힘
        stateMachine.ChangeState(new DoorClosedState());
    }

    public void Open()
    {
        // 닫혀있을 때만 열기
        if (stateMachine.CurrentState is DoorClosedState)
        {
            stateMachine.ChangeState(new DoorOpeningState());
        }
    }

    public void Close()
    {
        // 열려있을 때만 닫기
        if (stateMachine.CurrentState is DoorOpenedState)
        {
            stateMachine.ChangeState(new DoorClosingState());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Open();
        }
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}
```

---

## 주의사항

### ⚠️ 반드시 Dispose 호출

```csharp
// ❌ 나쁜 예: Dispose 호출 안 함
public class Player : MonoBehaviour
{
    private StateMachine<PlayerContext> stateMachine;

    private void Awake()
    {
        stateMachine = new StateMachine<PlayerContext>(new PlayerContext());
    }

    // OnDestroy 없음 → 메모리 누수!
}

// ✅ 좋은 예: Dispose 호출
public class Player : MonoBehaviour
{
    private StateMachine<PlayerContext> stateMachine;

    private void Awake()
    {
        stateMachine = new StateMachine<PlayerContext>(new PlayerContext());
    }

    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}
```

### ⚠️ Context는 class 타입만 가능

```csharp
// ❌ 잘못된 예: struct 사용
public struct PlayerContext { } // 컴파일 오류!

// ✅ 올바른 예: class 사용
public class PlayerContext { }
```

### ⚠️ 상태 전환은 Update 내에서

```csharp
// ❌ 나쁜 예: Enter에서 즉시 전환
public class BadState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        // Enter에서 바로 전환하면 무한 루프 위험!
        context.StateMachine.ChangeState(new AnotherState());
    }
}

// ✅ 좋은 예: Update에서 조건부 전환
public class GoodState : IState<PlayerContext>
{
    public void Enter(PlayerContext context)
    {
        // 초기화만
    }

    public void Update(PlayerContext context, float deltaTime)
    {
        // 조건 체크 후 전환
        if (SomeCondition())
        {
            context.StateMachine.ChangeState(new AnotherState());
        }
    }
}
```

### ⚠️ null 체크

```csharp
// ❌ 나쁜 예: null 체크 없음
stateMachine.ChangeState(null); // 오류!

// ✅ 좋은 예: null 체크
var nextState = GetNextState();
if (nextState != null)
{
    stateMachine.ChangeState(nextState);
}
```

### ⚠️ GameFlowManager 의존성

StateMachine은 자동으로 GameFlowManager에 등록됩니다. GameFlowManager가 초기화되어 있어야 합니다.

```csharp
// GameFlowManager가 씬에 있어야 함!
```

---

## FAQ

### Q1. 언제 StateMachine을 사용하나요?

A. 다음과 같은 경우에 사용합니다:
- ✅ 오브젝트가 **여러 상태**를 가질 때 (Idle, Run, Jump 등)
- ✅ 상태별로 **다른 동작**을 해야 할 때
- ✅ **상태 전환 조건**이 복잡할 때
- ✅ AI 로직, 게임 상태 관리, UI 흐름 등

**사용하지 않아도 되는 경우:**
- ❌ 상태가 1~2개만 있는 간단한 경우
- ❌ if-else로 충분히 관리 가능한 경우

### Q2. 상태를 싱글톤으로 만들 수 있나요?

A. 가능하지만 권장하지 않습니다. 상태는 가볍게 만들고 필요할 때 생성하는 것이 좋습니다.

```csharp
// ❌ 권장하지 않음: 싱글톤 상태
public class IdleState : IState<PlayerContext>
{
    public static readonly IdleState Instance = new IdleState();
}

// ✅ 권장: 필요할 때 생성
stateMachine.ChangeState(new IdleState());
```

### Q3. 상태에서 다른 상태로 직접 전환할 수 있나요?

A. 네, Context에 StateMachine 참조를 넣으면 가능합니다.

```csharp
public void Update(PlayerContext context, float deltaTime)
{
    if (SomeCondition())
    {
        context.StateMachine.ChangeState(new NextState());
    }
}
```

### Q4. 이전 상태는 언제까지 유지되나요?

A. 다음 상태 전환까지 유지됩니다.

```csharp
stateMachine.ChangeState(new StateA());
// PreviousState = null, CurrentState = StateA

stateMachine.ChangeState(new StateB());
// PreviousState = StateA, CurrentState = StateB

stateMachine.ChangeState(new StateC());
// PreviousState = StateB, CurrentState = StateC
```

### Q5. 상태 전환 검증은 언제 사용하나요?

A. 특정 조건에서만 상태 전환을 허용하고 싶을 때 사용합니다.

```csharp
// 예: 스턴 상태에서는 다른 상태로 전환 불가
stateMachine.TransitionValidator = (from, to) =>
{
    if (from is StunnedState)
    {
        return false; // 스턴 중에는 전환 불가
    }
    return true;
};
```

### Q6. Update가 호출되지 않습니다.

A. GameFlowManager가 씬에 있는지 확인하세요. StateMachine은 자동으로 GameFlowManager에 등록되므로, GameFlowManager가 없으면 Update가 호출되지 않습니다.

### Q7. 여러 StateMachine을 동시에 사용할 수 있나요?

A. 네, 가능합니다. 각 StateMachine은 독립적으로 동작합니다.

```csharp
public class Player : MonoBehaviour
{
    private StateMachine<MovementContext> movementSM;
    private StateMachine<CombatContext> combatSM;

    private void Awake()
    {
        // 이동 상태 머신
        movementSM = new StateMachine<MovementContext>(new MovementContext());
        movementSM.ChangeState(new IdleState());

        // 전투 상태 머신
        combatSM = new StateMachine<CombatContext>(new CombatContext());
        combatSM.ChangeState(new NormalState());
    }

    private void OnDestroy()
    {
        movementSM?.Dispose();
        combatSM?.Dispose();
    }
}
```

---

## 요약

**StateMachine 사용 4단계:**

1. **Context 클래스 정의** (공유 데이터)
2. **IState 구현** (각 상태 클래스)
3. **StateMachine 생성 및 초기 상태 설정**
4. **Dispose 호출** (정리)

```csharp
// 1. Context 정의
public class PlayerContext
{
    public Transform Transform;
    public float MoveSpeed;
    public StateMachine<PlayerContext> StateMachine;
}

// 2. State 구현
public class IdleState : IState<PlayerContext>
{
    public void Enter(PlayerContext context) { }
    public void Update(PlayerContext context, float deltaTime) { }
    public void Exit(PlayerContext context) { }
}

// 3. StateMachine 생성
public class Player : MonoBehaviour
{
    private StateMachine<PlayerContext> stateMachine;

    private void Awake()
    {
        var context = new PlayerContext { Transform = transform, MoveSpeed = 5f };
        stateMachine = new StateMachine<PlayerContext>(context);
        context.StateMachine = stateMachine;

        stateMachine.ChangeState(new IdleState());
    }

    // 4. Dispose
    private void OnDestroy()
    {
        stateMachine?.Dispose();
    }
}
```

**핵심 원칙:**
- Context는 상태들이 공유하는 데이터
- 각 상태는 독립적인 클래스로 분리
- GameFlowManager와 자동 통합 (Update 호출)
- 반드시 Dispose 호출

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/StateMachine/StateMachine.cs`
- 인터페이스: `Assets/Scripts/Core/StateMachine/IState.cs`
- 예제: `Assets/Scripts/Core/StateMachine/Example/StateMachineExample.cs`
