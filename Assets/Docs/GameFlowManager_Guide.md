# GameFlowManager 사용 가이드

## 개요

GameFlowManager는 게임의 Update/FixedUpdate/LateUpdate를 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ 실행 순서를 명확하게 제어 가능
- ✅ MonoBehaviour 없이 일반 C# 클래스로 작성 가능
- ✅ 메모리 누수 자동 경고

---

## 기본 사용법

### 1. 베이스 클래스 상속 (권장)

```csharp
public class Player : UpdatableBase
{
    // 실행 우선순위 (낮을수록 먼저 실행)
    public override int UpdateOrder => 100;

    public void Initialize()
    {
        Register(); // GameFlowManager에 등록
    }

    public override void OnUpdate(float deltaTime)
    {
        // 플레이어 로직
    }
}

// 사용
var player = new Player();
player.Initialize();

// 게임 종료 시 반드시 호출!
player.Dispose();
```

**⚠️ Dispose()를 호출하지 않으면 GC 시 경고가 출력됩니다!**

### 2. 인터페이스 선택

| 베이스 클래스 | 용도 | 예시 |
|--------------|------|------|
| `UpdatableBase` | 매 프레임 실행 | 입력 처리, AI |
| `FixedUpdatableBase` | 고정 간격 실행 | 물리 연산 |
| `LateUpdatableBase` | Update 이후 실행 | 카메라 추적 |

---

## 실행 순서 제어

`UpdateOrder` 값으로 실행 순서를 제어합니다. **낮은 값이 먼저 실행됩니다.**

```csharp
public class InputManager : UpdatableBase
{
    public override int UpdateOrder => 0; // 가장 먼저
}

public class Player : UpdatableBase
{
    public override int UpdateOrder => 100; // 입력 처리 후
}

public class UIManager : UpdatableBase
{
    public override int UpdateOrder => 200; // 플레이어 로직 후
}
```

**권장 우선순위:**
- 0~99: 입력 처리
- 100~199: 게임 로직
- 200~299: UI 업데이트

---

## 예제 모음

### 물리 연산

```csharp
public class PhysicsController : FixedUpdatableBase
{
    public override int FixedUpdateOrder => 100;
    private Rigidbody rb;

    public void Initialize(Rigidbody rigidbody)
    {
        rb = rigidbody;
        Register();
    }

    public override void OnFixedUpdate(float fixedDeltaTime)
    {
        rb.AddForce(Vector3.forward * 10f);
    }
}
```

### 카메라 추적

```csharp
public class CameraController : LateUpdatableBase
{
    public override int LateUpdateOrder => 0;
    private Transform target;
    private Camera camera;

    public void Initialize(Transform target, Camera camera)
    {
        this.target = target;
        this.camera = camera;
        Register();
    }

    public override void OnLateUpdate(float deltaTime)
    {
        // 플레이어 이동 후 카메라 추적
        camera.transform.position = target.position + new Vector3(0, 5, -10);
    }
}
```

### MonoBehaviour와 함께 사용

```csharp
public class PlayerController : MonoBehaviour
{
    private Player player;

    void Awake()
    {
        player = new Player();
        player.Initialize();
    }

    void OnDestroy()
    {
        player?.Dispose(); // 반드시 호출!
    }
}
```

---

## 일시정지

```csharp
// 일시정지
GameFlowManager.Instance.IsPaused = true;

// 재개
GameFlowManager.Instance.IsPaused = false;
```

---

## 디버깅 (에디터 전용)

```csharp
// 등록된 모든 객체 확인
GameFlowManager.Instance.LogRegisteredObjects();

// 등록된 객체 수 확인
int count = GameFlowManager.Instance.GetRegisteredObjectCount();
```

---

## 주의사항

### ⚠️ Dispose() 필수 호출

```csharp
// ❌ 나쁜 예
void SpawnEnemy()
{
    var enemy = new Enemy();
    enemy.Initialize();
    // Dispose()를 호출하지 않음 → 메모리 누수!
}

// ✅ 좋은 예
public class EnemyManager
{
    private List<Enemy> enemies = new List<Enemy>();

    public void SpawnEnemy()
    {
        var enemy = new Enemy();
        enemy.Initialize();
        enemies.Add(enemy);
    }

    public void Clear()
    {
        foreach (var enemy in enemies)
        {
            enemy.Dispose(); // 반드시 호출!
        }
        enemies.Clear();
    }
}
```

### ⚠️ Update 실행 중 등록/해제

Update 실행 중에도 안전하게 등록/해제할 수 있습니다.

```csharp
public override void OnUpdate(float deltaTime)
{
    if (health <= 0)
    {
        Dispose(); // 안전하게 해제 가능
    }
}
```

---

## 요약

**GameFlowManager 사용 3단계:**

1. **베이스 클래스 상속** (UpdatableBase, FixedUpdatableBase, LateUpdatableBase)
2. **Initialize()에서 Register() 호출**
3. **사용 완료 후 Dispose() 호출**

```csharp
public class MyObject : UpdatableBase
{
    public override int UpdateOrder => 100;

    public void Initialize()
    {
        Register();
    }

    public override void OnUpdate(float deltaTime)
    {
        // 로직
    }
}
```

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/GameFlowManager.cs`
- 관련 인터페이스: `Assets/Scripts/Core/Interfaces/`
