# Implementation Rules

## Initialization

- Minimize `MonoBehaviour` usage.
- Prefer plain C# classes when Unity lifecycle hooks are not required.
- Use an explicit `Initialize()` method for setup.
- If `Awake()` or `Start()` is required, call `Initialize()` internally.

```csharp
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

public class PlayerView : MonoBehaviour
{
    private Renderer playerRenderer;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 초기화 시 한 번만 컴포넌트를 확보합니다.
        playerRenderer = GetComponent<Renderer>();
    }
}
```

## Update flow

- Prefer `GameFlowManager` over direct `Update()` usage.
- Implement `IUpdatable` or `IFixedUpdatable` for recurring update logic.
- Register and unregister explicitly.

```csharp
public class Player : IUpdatable
{
    private float moveSpeed;

    public void Initialize(float speed)
    {
        moveSpeed = speed;
        GameFlowManager.Instance.RegisterUpdatable(this);
    }

    public void OnUpdate(float deltaTime)
    {
        // 플레이어 업데이트 로직
    }

    public void Dispose()
    {
        GameFlowManager.Instance.UnregisterUpdatable(this);
    }
}
```

## Async

- Use `UniTask` instead of coroutines for async work.
- Pass `CancellationToken` into async methods by default.
- Minimize `UniTaskVoid`.

```csharp
public async UniTask<bool> AttackAsync(CancellationToken cancellationToken)
{
    await UniTask.Delay(500, cancellationToken: cancellationToken);
    return true;
}
```

## Performance

- Do not call `GetComponent<T>()` repeatedly.
- Prefer dependency injection or `SerializeField` assignment.
- If needed, cache the component once in `Awake()` or `Initialize()`.
- Minimize `Find()`-style runtime lookups.
- Do not use LINQ. Use explicit loops and manual collection handling.
- Use pooling for frequently spawned/despawned objects.
- Tie async work to `CancellationToken` to avoid leaks.

```csharp
var activePlayers = new List<Player>(players.Count);
foreach (var player in players)
{
    if (player.IsActive)
    {
        activePlayers.Add(player);
    }
}
```

## Resource loading

- Do not use the `Resources` folder.
- Use the Addressable pipeline for dynamic asset loading.
- Release loaded assets according to project rules.

```csharp
var loader = AddressableLoader.Instance;
var handle = await loader.LoadAssetAsync<GameObject>("Prefabs/Player", cancellationToken);
loader.ReleaseAsset(handle);
```

## Component structure

- Keep responsibilities narrow.
- Prefer interfaces for loose coupling.
- Separate View and Logic where practical.
