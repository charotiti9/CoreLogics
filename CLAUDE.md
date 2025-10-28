# AI 개발 가이드

## 개발 프로세스

### 1. 요구사항 접수
- 사용자로부터 개발 요청을 받습니다.

### 2. 개발 문서 작성 및 제시
- 요구사항을 분석하여 **개발 문서**를 먼저 작성합니다.
- 개발 문서에는 다음 내용을 포함합니다:
  - 기능 개요
  - 구현 방법 및 접근 방식
  - 파일 구조 및 클래스 설계
  - 주요 메서드 및 로직 설명
  - 예상되는 영향 범위

### 3. 검토 및 승인
- 사용자가 개발 문서를 검토합니다.
- 수정이 필요한 경우 피드백을 받아 문서를 수정합니다.
- 사용자의 최종 승인을 받습니다.

### 4. 구현
- 승인받은 개발 문서를 기반으로 코드를 작성합니다.

---

## 코딩 규칙

### 객체지향 개발 원칙
- **SOLID 원칙** 준수
  - Single Responsibility: 클래스는 하나의 책임만 가집니다.
  - Open/Closed: 확장에는 열려있고 수정에는 닫혀있어야 합니다.
  - Liskov Substitution: 파생 클래스는 기반 클래스를 대체할 수 있어야 합니다.
  - Interface Segregation: 클라이언트가 사용하지 않는 메서드에 의존하지 않아야 합니다.
  - Dependency Inversion: 추상화에 의존해야 하며, 구체화에 의존하지 않아야 합니다.

### 코드 품질
- **유지보수 용이성**: 코드 변경 시 영향 범위를 최소화합니다.
- **가독성**: 명확한 네이밍과 구조로 코드의 의도를 쉽게 파악할 수 있도록 합니다.
- **일관성**: 프로젝트 전체에서 일관된 코딩 스타일을 유지합니다.

### 네이밍 규칙
- **클래스명**: PascalCase (예: `PlayerController`, `GameManager`)
- **메서드명**: PascalCase (예: `GetPlayerHealth()`, `UpdateScore()`)
- **public 변수**: PascalCase (예: `Health`, `CurrentScore`)
- **private 변수**: camelCase (예: `health`, `currentScore`)
- **private backing field**: _camelCase (예: `_health`, `_currentScore`)
  - public 프로퍼티에서 반환하는 private 필드에만 언더바 사용
- **상수명**: UPPER_SNAKE_CASE (예: `MAX_HEALTH`, `DEFAULT_SPEED`)

```csharp
public class Player
{
    // public 프로퍼티에서 반환하는 private 필드 (backing field)
    private int _health;
    public int Health
    {
        get => _health;
        private set => _health = value;
    }

    // 일반 private 변수
    private float moveSpeed;

    // public 변수
    public string PlayerName;

    // 상수
    private const int MAX_HEALTH = 100;
}
```

### 주석 규칙
- **언어**: 한글로 작성합니다.
- **목적**: 코드의 의도와 이유를 설명합니다.
- **작성 대상**:
  - 복잡한 로직
  - 비즈니스 규칙
  - 외부 의존성이나 제약사항
  - public 메서드 및 클래스 (XML 문서 주석 권장)

```csharp
/// <summary>
/// 플레이어의 체력을 감소시킵니다.
/// </summary>
/// <param name="damage">받을 데미지 값</param>
/// <returns>플레이어가 생존 중이면 true, 사망하면 false</returns>
public bool TakeDamage(float damage)
{
    // 체력이 0 이하로 떨어지지 않도록 보정
    _health = Mathf.Max(0, _health - damage);

    return _health > 0;
}
```

---

## Git 커밋 규칙

### 커밋 메시지 언어
- **한글**로 작성합니다.

### 커밋 메시지 형식
```
type: 간단한 설명

상세 설명 (선택사항)
```

### 커밋 타입
- `feat:` - 새로운 기능 추가
- `fix:` - 버그 수정
- `refactor:` - 코드 구조 개선 (기능 변경 없음)
- `docs:` - 문서 추가/수정
- `test:` - 테스트 코드 추가/수정
- `style:` - 코드 포매팅, 세미콜론 누락 등
- `chore:` - 빌드, 설정 파일 수정

### 커밋 예시
```
feat: 플레이어 점프 기능 추가

- Jump() 메서드 구현
- 공중에서 점프 불가능하도록 isGrounded 체크 추가
```

---

## Unity 특화 가이드

### 초기화 패턴
- **MonoBehaviour 사용 최소화**: 가능한 한 일반 C# 클래스로 구현합니다.
- **명시적 초기화**: `Initialize()` 메서드를 만들어 명시적으로 초기화합니다.
- `Awake()`, `Start()`를 사용해야 하는 경우에도 내부에서 `Initialize()`를 호출하도록 합니다.

```csharp
// MonoBehaviour를 사용하지 않는 일반 클래스 (권장)
public class GameData
{
    private int score;
    private string playerName;

    public void Initialize(string name)
    {
        playerName = name;
        score = 0;
    }
}

// MonoBehaviour를 사용해야 하는 경우
public class PlayerView : MonoBehaviour
{
    private Renderer playerRenderer;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 초기화 로직
        playerRenderer = GetComponent<Renderer>();
    }
}
```

### 중앙집중식 게임 플로우 관리
- `GameFlowManager`와 같은 매니저를 통해 Update, FixedUpdate 등을 중앙에서 관리합니다.
- 각 컴포넌트는 Update 대신 매니저에 콜백을 등록하는 방식을 사용합니다.
- **목적**: 실행 순서를 명확히 파악하고 제어하기 위함

```csharp
// 업데이트 가능한 객체 인터페이스
public interface IUpdatable
{
    void OnUpdate(float deltaTime);
}

public interface IFixedUpdatable
{
    void OnFixedUpdate(float fixedDeltaTime);
}

// 중앙 게임 플로우 매니저
public class GameFlowManager : MonoBehaviour
{
    private List<IUpdatable> updatables = new List<IUpdatable>();
    private List<IFixedUpdatable> fixedUpdatables = new List<IFixedUpdatable>();

    public void RegisterUpdatable(IUpdatable updatable)
    {
        if (!updatables.Contains(updatable))
            updatables.Add(updatable);
    }

    public void UnregisterUpdatable(IUpdatable updatable)
    {
        updatables.Remove(updatable);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        // 명확한 실행 순서 제어
        foreach (var updatable in updatables)
        {
            updatable.OnUpdate(deltaTime);
        }
    }

    private void FixedUpdate()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;

        foreach (var fixedUpdatable in fixedUpdatables)
        {
            fixedUpdatable.OnFixedUpdate(fixedDeltaTime);
        }
    }
}

// 사용 예시
public class Player : IUpdatable
{
    private float moveSpeed;

    public void Initialize(float speed)
    {
        moveSpeed = speed;
    }

    public void OnUpdate(float deltaTime)
    {
        // 플레이어 업데이트 로직
    }
}
```

### UniTask 활용
- **코루틴 대신 UniTask 사용**: 성능 최적화와 가독성 향상을 위해 UniTask를 적극 활용합니다.
- **비동기 처리**: 파일 로드, 네트워크 통신, 씬 로딩 등 무거운 작업은 UniTask로 비동기 처리합니다.
- **메모리 할당 최소화**: UniTask는 struct 기반으로 GC Allocation이 0입니다.

```csharp
// 코루틴 대신 UniTask 사용
public async UniTask LoadDataAsync(CancellationToken cancellationToken)
{
    // 비동기 로딩
    var data = await Resources.LoadAsync<GameObject>("Prefabs/Player");

    // 딜레이 (코루틴의 WaitForSeconds 대체)
    await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: cancellationToken);

    // 다음 프레임 대기
    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
}

// CancellationToken을 활용한 안전한 취소
public class GameController
{
    private CancellationTokenSource cts;

    public void Initialize()
    {
        cts = new CancellationTokenSource();
        LoadGameAsync(cts.Token).Forget();
    }

    private async UniTaskVoid LoadGameAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadDataAsync(cancellationToken);
            await InitializeGameAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // 취소 처리
        }
    }

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

**UniTask 사용 가이드:**
- `UniTask<T>`: 반환 값이 있는 비동기 메서드
- `UniTask`: 반환 값이 없는 비동기 메서드
- `UniTaskVoid`: Fire-and-forget 방식 (반환 값 무시, 예외 처리 주의)
- `CancellationToken`: 항상 파라미터로 받아 안전하게 취소 가능하도록 구현
- `.Forget()`: UniTaskVoid 대신 사용 가능하지만, 예외 처리 주의

```csharp
// 좋은 예시: CancellationToken 활용
public async UniTask<bool> AttackAsync(CancellationToken cancellationToken)
{
    await UniTask.Delay(500, cancellationToken: cancellationToken);
    return true;
}

// 나쁜 예시: CancellationToken 없이 사용
public async UniTask AttackAsync()
{
    await UniTask.Delay(500); // 취소 불가능
}
```

### 성능 고려사항
- `GetComponent<T>()`는 비용이 높으므로 캐싱합니다.
- `Find()` 계열 메서드는 런타임에 최소화합니다.
- 오브젝트 풀링을 활용하여 빈번한 생성/파괴를 방지합니다.
- **코루틴 대신 UniTask 사용**으로 GC Allocation 0 달성
- 비동기 작업은 항상 CancellationToken과 함께 사용하여 메모리 누수 방지

### 컴포넌트 구조
- 단일 책임 원칙에 따라 컴포넌트를 분리합니다.
- 느슨한 결합을 위해 인터페이스를 활용합니다.
- View와 Logic을 분리하여 테스트와 유지보수를 용이하게 합니다.

---

## 체크리스트

개발 완료 전 확인사항:
- [ ] 개발 문서에 명시된 모든 기능이 구현되었는가?
- [ ] 주석이 한글로 작성되었는가?
- [ ] 변수/클래스명이 영어로 작성되었는가?
- [ ] 네이밍 규칙을 준수했는가? (public: PascalCase, private: camelCase, backing field: _camelCase)
- [ ] 코드가 SOLID 원칙을 준수하는가?
- [ ] 가독성과 유지보수성을 고려했는가?
- [ ] 커밋 메시지가 'type: 설명' 형식으로 한글로 작성되었는가?
- [ ] MonoBehaviour 사용을 최소화했는가?
- [ ] Initialize() 메서드를 통한 명시적 초기화를 구현했는가?
- [ ] GameFlowManager를 통한 중앙집중식 업데이트 구조를 사용했는가?
- [ ] Unity 성능 최적화를 고려했는가?
