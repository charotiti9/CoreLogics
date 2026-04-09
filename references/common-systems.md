# Common Systems

Use Common systems before building feature-local replacements.

## UI System

- Location: `Assets/Scripts/Common/UI/`
- Use `UIManager` and `UIBase` for UI lifecycle, layers, stacks, and dim handling.

```csharp
await UIManager.Instance.ShowAsync<MainMenuUI>(UILayer.Popup, cancellationToken);
UIManager.Instance.Hide<MainMenuUI>();
```

## Audio System

- Location: `Assets/Scripts/Common/Audio/`
- Use `AudioManager` for BGM, SFX, and Voice playback and control.

```csharp
await AudioManager.Instance.PlayBGMAsync("BGM_Title", cancellationToken);
AudioManager.Instance.PlaySFX("SFX_Click");
```

## Selection checklist

- Need UI? Use `UIManager` and `UIBase`.
- Need audio? Use `AudioManager`.
