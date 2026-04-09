# Core Systems

Use existing Core systems before introducing a new abstraction.

## Addressable System

- Location: `Assets/Scripts/Core/Addressable/`
- Use for centralized asset loading and release management.
- Prefer `AddressableLoader` over direct ad hoc loading.

## CSV System

- Location: `Assets/Scripts/Core/CSV/`
- Use for table-driven game data such as items, skills, stages, and configuration.
- Prefer `CSVManager` for loading and querying game data.

## Game System

- Location: `Assets/Scripts/Core/Game/`
- Use for bootstrap flow, global context, and game state entry points.

## GameFlow System

- Location: `Assets/Scripts/Core/GameFlow/`
- Use for centralized update registration and execution order control.

## Pool System

- Location: `Assets/Scripts/Core/Pool/`
- Use for repeated object creation and destruction patterns such as bullets, effects, and enemies.

## StateMachine System

- Location: `Assets/Scripts/Core/StateMachine/`
- Use for AI, gameplay state, and flow transitions.

## Singleton System

- Location: `Assets/Scripts/Core/Singleton/`
- Use only for true global managers.

## Selection checklist

- Need dynamic asset loading? Use `AddressableLoader`.
- Need game data tables? Use `CSVManager`.
- Need recurring updates? Use `GameFlowManager` with `IUpdatable`.
- Need repeated spawn/despawn? Use the pool system.
- Need state transitions? Use the state machine system.
- Need a global manager? Consider the singleton system carefully.
