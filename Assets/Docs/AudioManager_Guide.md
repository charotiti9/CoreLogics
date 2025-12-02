# Audio 시스템 사용 가이드

## 개요

Audio 시스템은 게임의 모든 사운드를 중앙에서 관리하는 시스템입니다.

**장점:**
- ✅ BGM, SFX, Voice 3개 채널 분리 관리
- ✅ 페이드 인/아웃 지원
- ✅ BGM 크로스페이드 전환
- ✅ 3D 공간 사운드 지원
- ✅ SFX 우선순위 큐
- ✅ Addressable과 통합 (리소스 관리)
- ✅ 볼륨 및 음소거 관리
- ✅ 설정 자동 저장/로드

---

## 핵심 개념

### 1. 3개 채널 시스템

오디오는 3개의 독립적인 채널로 관리됩니다.

```csharp
public enum AudioChannelType
{
    BGM,    // 배경 음악
    SFX,    // 효과음
    Voice   // 음성/대사
}
```

**채널별 특징:**
- **BGM**: 한 번에 하나만 재생, 페이드/크로스페이드 지원
- **SFX**: 동시에 여러 개 재생 가능, 우선순위 관리
- **Voice**: 대사 재생, 스킵 기능 지원

### 2. 볼륨 계층 구조

```
최종 볼륨 = Master × Channel × Local
```

- **Master**: 모든 사운드에 적용되는 마스터 볼륨
- **Channel**: BGM/SFX/Voice 각 채널의 볼륨
- **Local**: 개별 사운드의 볼륨

예: BGM의 최종 볼륨 = MasterVolume(0.8) × BGMVolume(0.6) × LocalVolume(1.0) = 0.48

### 3. Addressable 통합

모든 오디오 파일은 Addressable로 관리됩니다.
- 자동 로딩 및 해제
- 메모리 효율 최적화
- 중복 로드 방지

---

## 기본 사용법

### 1. BGM 재생

```csharp
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class GameStarter : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 기본 재생
        await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", ct: ct);

        // 페이드 인과 함께 재생 (2초에 걸쳐)
        await AudioManager.Instance.PlayBGMAsync(
            "Audio/BGM_Game",
            fadeInDuration: 2f,
            ct: ct
        );
    }
}
```

### 2. BGM 정지

```csharp
// 즉시 정지
await AudioManager.Instance.StopBGMAsync(ct: ct);

// 페이드 아웃 후 정지 (3초에 걸쳐)
await AudioManager.Instance.StopBGMAsync(fadeOutDuration: 3f, ct: ct);
```

### 3. BGM 일시정지/재개

```csharp
// 일시정지
AudioManager.Instance.PauseBGM();

// 재개
AudioManager.Instance.ResumeBGM();
```

### 4. BGM 크로스페이드

한 BGM에서 다른 BGM으로 부드럽게 전환합니다.

```csharp
// 타이틀 BGM → 게임 BGM으로 크로스페이드 (3초에 걸쳐)
await AudioManager.Instance.CrossFadeBGMAsync(
    "Audio/BGM_Game",
    duration: 3f,
    ct: ct
);
```

### 5. SFX 재생 (2D)

```csharp
// 기본 재생
await AudioManager.Instance.PlaySFXAsync("Audio/SFX_Click", ct: ct);

// 볼륨 지정
await AudioManager.Instance.PlaySFXAsync(
    "Audio/SFX_Explosion",
    volume: 0.8f,
    ct: ct
);

// 우선순위 지정 (높을수록 우선)
await AudioManager.Instance.PlaySFXAsync(
    "Audio/SFX_Important",
    volume: 1f,
    priority: 200,
    ct: ct
);
```

### 6. SFX 재생 (3D)

3D 공간에서 특정 위치에서 소리가 나도록 합니다.

```csharp
// 특정 위치에서 SFX 재생
Vector3 explosionPos = new Vector3(10, 0, 5);
await AudioManager.Instance.PlaySFXAtPositionAsync(
    "Audio/SFX_Explosion",
    explosionPos,
    volume: 1f,
    ct: ct
);
```

### 7. 모든 SFX 정지

```csharp
// 재생 중인 모든 SFX 즉시 정지
AudioManager.Instance.StopAllSFX();
```

### 8. Voice 재생

```csharp
// Voice 재생
await AudioManager.Instance.PlayVoiceAsync("Audio/Voice_Opening", ct: ct);

// Voice 완료 대기
var reason = await AudioManager.Instance.WaitForVoiceCompleteAsync(ct);

if (reason == VoiceCompleteReason.Completed)
{
    Debug.Log("Voice 정상 완료");
}
else if (reason == VoiceCompleteReason.Skipped)
{
    Debug.Log("Voice 스킵됨");
}
```

### 9. Voice 스킵

```csharp
// 사용자가 스페이스바를 누르면 Voice 스킵
private void Update()
{
    if (Input.GetKeyDown(KeyCode.Space))
    {
        AudioManager.Instance.SkipVoice();
    }
}
```

### 10. 볼륨 조절

```csharp
// Master 볼륨 (0.0 ~ 1.0)
AudioManager.Instance.MasterVolume = 0.8f;

// BGM 볼륨
AudioManager.Instance.BGMVolume = 0.6f;

// SFX 볼륨
AudioManager.Instance.SFXVolume = 0.7f;

// Voice 볼륨
AudioManager.Instance.VoiceVolume = 0.9f;
```

### 11. 음소거 (Mute)

```csharp
// Master 음소거
AudioManager.Instance.IsMasterMuted = true;

// BGM 음소거
AudioManager.Instance.IsBGMMuted = true;

// SFX 음소거
AudioManager.Instance.IsSFXMuted = true;

// Voice 음소거
AudioManager.Instance.IsVoiceMuted = true;
```

### 12. 설정 저장/로드

```csharp
// 설정 저장 (PlayerPrefs)
AudioManager.Instance.SaveSettings();

// 설정 로드 (게임 시작 시 자동 호출됨)
AudioManager.Instance.LoadSettings();

// 설정 초기화 (기본값으로 리셋)
AudioManager.Instance.ResetSettings();
```

---

## 실전 예제

### 게임 시작 시 BGM 재생

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class TitleScreen : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 타이틀 BGM 재생 (2초 페이드 인)
        await AudioManager.Instance.PlayBGMAsync(
            "Audio/BGM_Title",
            fadeInDuration: 2f,
            ct: ct
        );
    }

    public async UniTask StartGameAsync(CancellationToken ct)
    {
        // 게임 BGM으로 크로스페이드 (3초)
        await AudioManager.Instance.CrossFadeBGMAsync(
            "Audio/BGM_Game",
            duration: 3f,
            ct: ct
        );
    }
}
```

### 버튼 클릭 사운드

```csharp
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Common.Audio;

public class UIButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        // 클릭 사운드 재생
        PlayClickSoundAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid PlayClickSoundAsync(CancellationToken ct)
    {
        await AudioManager.Instance.PlaySFXAsync("Audio/SFX_Click", ct: ct);
    }
}
```

### 전투 사운드 (3D)

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class Weapon : MonoBehaviour
{
    public async UniTask AttackAsync(Vector3 targetPos, CancellationToken ct)
    {
        // 공격 SFX (3D 위치)
        await AudioManager.Instance.PlaySFXAtPositionAsync(
            "Audio/SFX_Sword",
            targetPos,
            volume: 0.8f,
            ct: ct
        );

        // 공격 로직...
    }
}

public class Explosion : MonoBehaviour
{
    public async UniTask ExplodeAsync(CancellationToken ct)
    {
        // 폭발 SFX (높은 우선순위)
        await AudioManager.Instance.PlaySFXAsync(
            "Audio/SFX_Explosion",
            volume: 1f,
            priority: 200,  // 높은 우선순위
            ct: ct
        );

        // 폭발 이펙트...
    }
}
```

### 대화 시스템

```csharp
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class DialogSystem : MonoBehaviour
{
    public Text dialogText;

    public async UniTask ShowDialogAsync(string voiceAddress, string text, CancellationToken ct)
    {
        // 대화 텍스트 표시
        dialogText.text = text;

        // Voice 재생
        await AudioManager.Instance.PlayVoiceAsync(voiceAddress, ct);

        // Voice 완료 대기 (스킵 가능)
        var reason = await AudioManager.Instance.WaitForVoiceCompleteAsync(ct);

        if (reason == VoiceCompleteReason.Skipped)
        {
            Debug.Log("대사 스킵됨");
        }

        // 대화 텍스트 숨김
        dialogText.text = "";
    }

    private void Update()
    {
        // 스페이스바로 Voice 스킵
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AudioManager.Instance.SkipVoice();
        }
    }
}

// 사용 예시
public class GameManager : MonoBehaviour
{
    public DialogSystem dialogSystem;

    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        await dialogSystem.ShowDialogAsync(
            "Audio/Voice_Opening",
            "안녕하세요, 모험가님!",
            ct
        );

        await dialogSystem.ShowDialogAsync(
            "Audio/Voice_Quest",
            "새로운 퀘스트가 있습니다.",
            ct
        );
    }
}
```

### 옵션 메뉴 (볼륨 슬라이더)

```csharp
using UnityEngine;
using UnityEngine.UI;
using Common.Audio;

public class OptionsMenu : MonoBehaviour
{
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;

    public Toggle masterMuteToggle;
    public Toggle bgmMuteToggle;
    public Toggle sfxMuteToggle;
    public Toggle voiceMuteToggle;

    private void Start()
    {
        // 초기값 설정
        masterSlider.value = AudioManager.Instance.MasterVolume;
        bgmSlider.value = AudioManager.Instance.BGMVolume;
        sfxSlider.value = AudioManager.Instance.SFXVolume;
        voiceSlider.value = AudioManager.Instance.VoiceVolume;

        masterMuteToggle.isOn = AudioManager.Instance.IsMasterMuted;
        bgmMuteToggle.isOn = AudioManager.Instance.IsBGMMuted;
        sfxMuteToggle.isOn = AudioManager.Instance.IsSFXMuted;
        voiceMuteToggle.isOn = AudioManager.Instance.IsVoiceMuted;

        // 이벤트 등록
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        voiceSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);

        masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);
        bgmMuteToggle.onValueChanged.AddListener(OnBGMMuteChanged);
        sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);
        voiceMuteToggle.onValueChanged.AddListener(OnVoiceMuteChanged);
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.MasterVolume = value;
    }

    private void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance.BGMVolume = value;
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SFXVolume = value;
    }

    private void OnVoiceVolumeChanged(float value)
    {
        AudioManager.Instance.VoiceVolume = value;
    }

    private void OnMasterMuteChanged(bool isMuted)
    {
        AudioManager.Instance.IsMasterMuted = isMuted;
    }

    private void OnBGMMuteChanged(bool isMuted)
    {
        AudioManager.Instance.IsBGMMuted = isMuted;
    }

    private void OnSFXMuteChanged(bool isMuted)
    {
        AudioManager.Instance.IsSFXMuted = isMuted;
    }

    private void OnVoiceMuteChanged(bool isMuted)
    {
        AudioManager.Instance.IsVoiceMuted = isMuted;
    }

    public void SaveSettings()
    {
        AudioManager.Instance.SaveSettings();
        Debug.Log("설정 저장됨");
    }

    public void ResetSettings()
    {
        AudioManager.Instance.ResetSettings();

        // UI 업데이트
        masterSlider.value = 1f;
        bgmSlider.value = 1f;
        sfxSlider.value = 1f;
        voiceSlider.value = 1f;

        masterMuteToggle.isOn = false;
        bgmMuteToggle.isOn = false;
        sfxMuteToggle.isOn = false;
        voiceMuteToggle.isOn = false;

        Debug.Log("설정 초기화됨");
    }
}
```

### 씬 전환 시 BGM 변경

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class SceneTransition : MonoBehaviour
{
    public async UniTask LoadSceneAsync(string sceneName, string newBGM, CancellationToken ct)
    {
        // 현재 BGM 페이드 아웃 (2초)
        await AudioManager.Instance.StopBGMAsync(fadeOutDuration: 2f, ct: ct);

        // 씬 로드
        await SceneManager.LoadSceneAsync(sceneName).ToUniTask(cancellationToken: ct);

        // 새 BGM 페이드 인 (2초)
        await AudioManager.Instance.PlayBGMAsync(newBGM, fadeInDuration: 2f, ct: ct);
    }
}
```

### 아이템 획득 사운드

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using Common.Audio;

public class Item : MonoBehaviour
{
    public string itemName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnPickupAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    private async UniTaskVoid OnPickupAsync(CancellationToken ct)
    {
        // 아이템 획득 SFX
        await AudioManager.Instance.PlaySFXAsync("Audio/SFX_ItemGet", ct: ct);

        Debug.Log($"{itemName} 획득!");

        // 아이템 제거
        Destroy(gameObject);
    }
}
```

### 보스 전투 BGM

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using Common.Audio;

public class BossBattle : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 일반 BGM → 보스 BGM으로 크로스페이드
        await AudioManager.Instance.CrossFadeBGMAsync(
            "Audio/BGM_Boss",
            duration: 2f,
            ct: ct
        );

        Debug.Log("보스 전투 시작!");
    }

    public async UniTask OnBossDefeatedAsync(CancellationToken ct)
    {
        // 승리 BGM으로 전환
        await AudioManager.Instance.PlayBGMAsync(
            "Audio/BGM_Victory",
            fadeInDuration: 1f,
            ct: ct
        );
    }
}
```

---

## 고급 기능

### SFX 우선순위

SFX가 최대 개수를 초과하면 우선순위가 낮은 SFX가 중단됩니다.

```csharp
// 중요한 SFX (높은 우선순위)
await AudioManager.Instance.PlaySFXAsync(
    "Audio/SFX_Critical",
    priority: 255,  // 최고 우선순위
    ct: ct
);

// 일반 SFX (보통 우선순위)
await AudioManager.Instance.PlaySFXAsync(
    "Audio/SFX_Normal",
    priority: 128,  // 기본값
    ct: ct
);

// 배경 SFX (낮은 우선순위)
await AudioManager.Instance.PlaySFXAsync(
    "Audio/SFX_Ambient",
    priority: 50,   // 낮은 우선순위
    ct: ct
);
```

**우선순위 범위:** 0 ~ 255 (255가 최고 우선순위)

---

## 주의사항

### ⚠️ Addressable 설정

오디오 파일을 Addressables에 등록해야 합니다.

1. 오디오 클립을 Addressables Groups에 추가
2. Address 설정 (예: `Audio/BGM_Title`, `Audio/SFX_Click`)

### ⚠️ CancellationToken 전달

```csharp
// ❌ 나쁜 예: CancellationToken 없음
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title");

// ✅ 좋은 예: CancellationToken 전달
var ct = this.GetCancellationTokenOnDestroy();
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", ct: ct);
```

### ⚠️ await 키워드 사용

```csharp
// ❌ 나쁜 예: await 없음 (페이드가 끝나기 전에 다음 코드 실행)
AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", fadeInDuration: 2f, ct: ct);
Debug.Log("즉시 실행됨!");

// ✅ 좋은 예: await로 완료 대기
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", fadeInDuration: 2f, ct: ct);
Debug.Log("페이드 인 완료 후 실행됨!");
```

### ⚠️ 설정 저장 시점

```csharp
// 옵션 메뉴를 닫을 때 저장
public void OnCloseOptionsMenu()
{
    AudioManager.Instance.SaveSettings();
}

// 게임 종료 시 자동 저장됨 (Dispose에서 호출)
```

### ⚠️ 동일 BGM 재재생

```csharp
// 이미 재생 중인 BGM을 다시 재생하려고 하면 무시됨
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", ct: ct);
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", ct: ct); // 무시됨

// 다른 BGM은 정상 재생
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Game", ct: ct); // 재생됨
```

---

## FAQ

### Q1. BGM과 SFX를 동시에 재생할 수 있나요?

A. 네, 가능합니다. 각 채널은 독립적으로 동작합니다.

```csharp
// BGM 재생
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Game", ct: ct);

// BGM 재생 중에 SFX도 재생 가능
await AudioManager.Instance.PlaySFXAsync("Audio/SFX_Click", ct: ct);
```

### Q2. SFX는 몇 개까지 동시 재생 가능한가요?

A. AudioConfig에서 설정한 `maxConcurrentSFX` 개수만큼 가능합니다. 기본값은 보통 10~20개입니다.

초과 시 우선순위가 낮은 SFX가 중단됩니다.

### Q3. BGM 루프는 자동인가요?

A. 네, BGM은 자동으로 루프 재생됩니다.

### Q4. Voice와 BGM을 동시에 재생하면?

A. 동시 재생됩니다. 필요하면 Voice 재생 시 BGM 볼륨을 줄일 수 있습니다.

```csharp
// Voice 재생 전 BGM 볼륨 감소
float originalVolume = AudioManager.Instance.BGMVolume;
AudioManager.Instance.BGMVolume = 0.3f;

await AudioManager.Instance.PlayVoiceAsync("Audio/Voice_Dialog", ct);

// Voice 완료 후 BGM 볼륨 복구
AudioManager.Instance.BGMVolume = originalVolume;
```

### Q5. 3D 사운드가 들리지 않습니다.

A. 다음을 확인하세요:
1. AudioListener가 씬에 있는지 (보통 Main Camera에 있음)
2. 사운드 재생 위치가 AudioListener 범위 내인지
3. AudioClip의 Spatial Blend가 3D로 설정되어 있는지

### Q6. 페이드 없이 즉시 전환하려면?

A. `fadeInDuration`과 `fadeOutDuration`을 0으로 설정합니다.

```csharp
// 즉시 BGM 변경
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Game", fadeInDuration: 0f, ct: ct);
```

### Q7. 설정이 저장되지 않습니다.

A. `SaveSettings()`를 호출해야 합니다. 게임 종료 시 자동으로 호출되지만, 명시적으로 호출하는 것이 안전합니다.

```csharp
AudioManager.Instance.SaveSettings();
```

---

## 요약

**Audio 시스템 사용 3단계:**

1. **BGM 재생**
2. **SFX 재생**
3. **볼륨 조절**

```csharp
// 1. BGM 재생
await AudioManager.Instance.PlayBGMAsync("Audio/BGM_Title", ct: ct);

// 2. SFX 재생
await AudioManager.Instance.PlaySFXAsync("Audio/SFX_Click", ct: ct);

// 3. 볼륨 조절
AudioManager.Instance.BGMVolume = 0.7f;
AudioManager.Instance.SFXVolume = 0.8f;
```

**페이드 사용:**
```csharp
// 페이드 인
await AudioManager.Instance.PlayBGMAsync(
    "Audio/BGM_Game",
    fadeInDuration: 2f,
    ct: ct
);

// 페이드 아웃
await AudioManager.Instance.StopBGMAsync(fadeOutDuration: 2f, ct: ct);

// 크로스페이드
await AudioManager.Instance.CrossFadeBGMAsync(
    "Audio/BGM_Boss",
    duration: 3f,
    ct: ct
);
```

**핵심 원칙:**
- 3개 채널 (BGM, SFX, Voice) 독립 관리
- Addressable로 리소스 자동 관리
- 페이드/크로스페이드로 부드러운 전환
- CancellationToken 필수 전달
- 설정은 자동 저장/로드

**추가 정보:**
- 소스 코드: `Assets/Scripts/Common/Audio/Core/AudioManager.cs`
- 채널 관리: `Assets/Scripts/Common/Audio/Core/AudioChannel.cs`
- 설정 관리: `Assets/Scripts/Common/Audio/Core/AudioSettings.cs`
