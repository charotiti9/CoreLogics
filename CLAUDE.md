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
- **LINQ 사용 금지**: 성능 문제로 인해 LINQ는 사용하지 않습니다.
  - `foreach` 루프와 명시적 컬렉션 조작을 사용합니다.
  - LINQ는 GC Allocation과 성능 오버헤드를 발생시킵니다.

```csharp
// ❌ 나쁜 예시: LINQ 사용
var activePlayers = players.Where(p => p.IsActive).ToList();
var topScore = players.Max(p => p.Score);

// ✅ 좋은 예시: 명시적 루프 사용
var activePlayers = new List<Player>(players.Count);
foreach (var player in players)
{
    if (player.IsActive)
    {
        activePlayers.Add(player);
    }
}

int topScore = 0;
foreach (var player in players)
{
    if (player.Score > topScore)
    {
        topScore = player.Score;
    }
}
```

### 리소스 로딩
- **Resources 폴더 사용 금지**: Resources 폴더는 사용하지 않습니다.
  - Resources 폴더의 모든 에셋은 빌드에 포함되어 빌드 크기가 증가합니다.
  - 런타임에 선택적 로딩/언로딩이 불가능하여 메모리 관리가 어렵습니다.
  - 의존성 추적이 불가능하여 유지보수가 어렵습니다.
- **Addressable Asset System 사용**: 모든 동적 리소스 로딩은 Addressable을 사용합니다.
  - 선택적 다운로드 및 패치 지원
  - 메모리 관리 용이 (참조 카운팅)
  - 의존성 자동 추적

```csharp
// ❌ 나쁜 예시: Resources 사용
var prefab = Resources.Load<GameObject>("Prefabs/Player");
var sprite = Resources.LoadAsync<Sprite>("UI/Icon");

// ✅ 좋은 예시: Addressable 사용
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Player");
var prefab = await handle.ToUniTask(cancellationToken: ct);

// 사용 후 반드시 Release
Addressables.Release(handle);
```

### 컴포넌트 구조
- 단일 책임 원칙에 따라 컴포넌트를 분리합니다.
- 느슨한 결합을 위해 인터페이스를 활용합니다.
- View와 Logic을 분리하여 테스트와 유지보수를 용이하게 합니다.

### 프로젝트 시스템 활용
프로젝트에는 이미 구축된 강력한 시스템들이 `Assets/Scripts/Common`과 `Assets/Scripts/Core` 폴더에 준비되어 있습니다. **새로운 기능을 개발할 때는 반드시 이러한 기존 시스템들을 최대한 활용**해야 합니다.

#### Scripts/Core/ - 핵심 시스템
핵심 게임 인프라를 제공하는 시스템들입니다. 새로운 기능 개발 시 우선적으로 활용해야 합니다.

**1. Addressable System** (`Core/Addressable/`)
- **AddressableLoader**: 모든 리소스 로딩의 중앙 관리자
  - 참조 카운팅으로 자동 메모리 관리
  - 중복 로딩 방지
  - 비동기 로딩 지원 (UniTask 통합)
- **사용 예시**:
```csharp
// Addressable을 통한 리소스 로딩
var loader = AddressableLoader.Instance;
var handle = await loader.LoadAssetAsync<GameObject>("Prefabs/Player", cancellationToken);

// 사용 완료 후 자동 Release (참조 카운팅)
loader.ReleaseAsset(handle);
```

**2. CSV System** (`Core/CSV/`)
- **CSVManager**: 게임 데이터 테이블 관리
  - CSV 파일 자동 파싱 및 C# 클래스 생성
  - 데이터 간 참조 자동 해결
  - 순환 참조 검증
- **사용 시점**: 게임 데이터 (아이템, 스킬, 스테이지 등)를 관리할 때
- **사용 예시**:
```csharp
// CSV 데이터 로드
await CSVManager.Instance.LoadAllTablesAsync(cancellationToken);

// 데이터 조회
var itemData = CSVManager.Instance.GetTable<ItemData>();
var item = itemData.GetById(itemId);
```

**3. Game System** (`Core/Game/`)
- **GameBootstrap**: 게임 초기화 진입점
  - 모든 시스템의 초기화 순서 관리
  - 게임 상태 전환 제어
- **GameContext**: 게임 전역 데이터 컨테이너
  - 게임 세션 데이터 보관
- **사용 시점**: 새로운 게임 상태나 초기화 로직 추가 시

**4. GameFlow System** (`Core/GameFlow/`)
- **GameFlowManager**: 중앙집중식 Update 관리
  - IUpdatable, IFixedUpdatable 인터페이스 제공
  - 실행 순서를 명확하게 제어
- **필수 사용**: MonoBehaviour의 Update() 대신 이 시스템 사용
- **사용 예시**:
```csharp
public class Enemy : IUpdatable
{
    public void Initialize()
    {
        GameFlowManager.Instance.RegisterUpdatable(this);
    }

    public void OnUpdate(float deltaTime)
    {
        // 적 업데이트 로직
    }

    public void Dispose()
    {
        GameFlowManager.Instance.UnregisterUpdatable(this);
    }
}
```

**5. Pool System** (`Core/Pool/`)
- 오브젝트 풀링 시스템 (Addressable 통합)
- 빈번한 생성/파괴가 필요한 오브젝트에 사용
- **사용 시점**: 총알, 이펙트, 적 등 반복 생성되는 오브젝트

**6. StateMachine System** (`Core/StateMachine/`)
- 범용 상태 머신 구현
- **사용 시점**: 캐릭터 AI, 게임 상태 전환 등

**7. Singleton System** (`Core/Singleton/`)
- 싱글톤 패턴 구현 (MonoBehaviour/일반 클래스)
- **주의**: 남용 금지, 진짜 전역 관리자에만 사용

#### Scripts/Common/ - 공통 기능
게임 전반에서 사용되는 공통 기능들입니다.

**1. UI System** (`Common/UI/`)
- **UIManager**: UI 생명주기 및 레이어 관리
  - UI 열기/닫기 자동 관리
  - UI 레이어 시스템 (Popup, HUD 등)
  - UI 스택 관리
  - Dim(배경 어둡게) 자동 처리
- **UIBase**: 모든 UI의 기반 클래스
  - 생명주기 메서드 제공 (OnShow, OnHide 등)
- **사용 예시**:
```csharp
// UI 열기
await UIManager.Instance.ShowAsync<MainMenuUI>(UILayer.Popup, cancellationToken);

// UI 닫기
UIManager.Instance.Hide<MainMenuUI>();

// 모든 UI 닫기
UIManager.Instance.HideAll();
```

**2. Audio System** (`Common/Audio/`)
- **AudioManager**: 오디오 재생 및 관리
  - BGM, SFX, Voice 채널 분리
  - 페이드 인/아웃 지원
  - 우선순위 큐 관리
  - Addressable 통합
- **사용 예시**:
```csharp
// BGM 재생
await AudioManager.Instance.PlayBGMAsync("BGM_Title", cancellationToken);

// SFX 재생
AudioManager.Instance.PlaySFX("SFX_Click");

// 볼륨 조절
AudioManager.Instance.SetVolume(AudioChannelType.BGM, 0.5f);
```

#### 시스템 활용 체크리스트
새로운 기능을 개발하기 전에 다음을 확인하세요:
- [ ] 리소스를 로딩해야 하는가? → **AddressableLoader 사용**
- [ ] 게임 데이터를 관리해야 하는가? → **CSVManager 사용**
- [ ] Update가 필요한가? → **GameFlowManager + IUpdatable 사용**
- [ ] 오브젝트를 반복 생성/파괴하는가? → **Pool System 사용**
- [ ] 상태 전환 로직이 있는가? → **StateMachine 사용**
- [ ] 전역 관리자가 필요한가? → **Singleton 사용 (남용 주의)**
- [ ] UI를 표시해야 하는가? → **UIManager + UIBase 사용**
- [ ] 사운드를 재생해야 하는가? → **AudioManager 사용**

#### 중요 원칙
1. **기존 시스템 우선 활용**: 바퀴를 재발명하지 마세요. 대부분의 기능은 이미 구현되어 있습니다.
2. **시스템 확장**: 기존 시스템으로 부족하다면, 새로운 시스템을 만들기 전에 기존 시스템을 확장할 수 있는지 검토하세요.
3. **일관성 유지**: 프로젝트 전체가 동일한 패턴과 시스템을 사용하도록 합니다.
4. **README 참조**: 각 시스템의 자세한 사용법은 `Assets/Scripts/README.md`를 참조하세요.

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
- [ ] LINQ를 사용하지 않고 명시적 루프를 사용했는가?
- [ ] Resources 폴더 대신 Addressable을 사용했는가?
