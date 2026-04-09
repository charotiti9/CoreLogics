---
name: corelogics-unity-dev
description: Use when working in the CoreLogics Unity project and implementation must follow the project's existing Core/Common systems and Unity-specific conventions. Apply this skill for feature development, refactoring, or bug fixing that should use AddressableLoader, CSVManager, GameFlowManager, Pool, StateMachine, UIManager, AudioManager, UniTask, explicit Initialize patterns, and the project's performance rules such as avoiding LINQ and avoiding Resources.
---

# CoreLogics Unity Development

Follow this skill when implementing or changing gameplay, UI, data flow, loading flow, or project systems in this repository. Use existing project infrastructure first and preserve the repository's established architectural patterns.

## Bundled files

Read only what is relevant to the task.

- `references/README.md`
  - Read first when you need the reference map for this skill.
- `references/implementation-rules.md`
  - Read when the task needs project implementation conventions such as initialization, async flow, performance constraints, or resource loading rules.
- `references/core-systems.md`
  - Read when the task touches Addressable, CSV, bootstrap/game flow, pooling, state machines, or singleton-backed managers.
- `references/common-systems.md`
  - Read when the task touches UI or audio systems.

If the bundled references are not enough, read `Assets/Scripts/README.md` and the relevant document under `Assets/Docs`.

## Core rules

- Prefer existing systems over building a new one.
- Check whether an existing Core/Common system can be extended before adding a new pattern.
- Keep implementation consistent with the rest of the project.
- Read `Assets/Scripts/README.md` and the relevant file in `Assets/Docs` when the task touches an existing system.

## Initialization pattern

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
- Register and unregister explicitly so execution order remains visible and controlled.

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

## Async pattern

- Use `UniTask` instead of coroutines for async work.
- Pass `CancellationToken` into async methods by default.
- Minimize `UniTaskVoid`; use it only when fire-and-forget is truly required and exception handling is clear.

```csharp
public async UniTask<bool> AttackAsync(CancellationToken cancellationToken)
{
    await UniTask.Delay(500, cancellationToken: cancellationToken);
    return true;
}
```

## Performance rules

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
- Release loaded assets according to the project's reference counting and release rules.

```csharp
var loader = AddressableLoader.Instance;
var handle = await loader.LoadAssetAsync<GameObject>("Prefabs/Player", cancellationToken);
loader.ReleaseAsset(handle);
```

## Component structure

- Keep responsibilities narrow.
- Prefer interfaces for loose coupling.
- Separate View and Logic where practical.

## Quick checklist

- Need dynamic asset loading? Use `AddressableLoader`.
- Need game data tables? Use `CSVManager`.
- Need recurring updates? Use `GameFlowManager` with `IUpdatable`.
- Need repeated spawn/despawn? Use the pool system.
- Need state transitions? Use the state machine system.
- Need a global manager? Consider the singleton system carefully.
- Need UI? Use `UIManager` and `UIBase`.
- Need audio? Use `AudioManager`.

## Final technical checks

- Minimize `MonoBehaviour`.
- Use explicit `Initialize()` patterns.
- Prefer centralized update flow.
- Follow performance constraints.
- Avoid LINQ.
- Avoid `Resources`.
