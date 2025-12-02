# Object Pool 시스템 가이드

## 개요

Object Pool 시스템은 게임 오브젝트의 반복적인 생성/파괴를 방지하여 성능을 최적화하는 시스템입니다.

**장점:**
- ✅ GC(Garbage Collection) 부담 대폭 감소
- ✅ 인스턴스 생성/파괴 비용 제거
- ✅ Attribute 기반 자동 관리
- ✅ AddressableLoader와 완벽 통합
- ✅ 프리팹 자동 캐싱 및 참조 카운팅

---

## 핵심 개념

### 1. Object Pooling이란?

빈번하게 생성/파괴되는 오브젝트를 재사용하는 패턴입니다.

**기존 방식 (비효율적):**
```csharp
// 총알 발사할 때마다 생성/파괴 → GC 발생!
var bullet = Instantiate(bulletPrefab);
// 사용 후
Destroy(bullet);
```

**Pool 방식 (효율적):**
```csharp
// 풀에서 가져오기 (재사용)
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
// 사용 후 풀로 반환
PoolManager.ReturnToPool(bullet);
```

### 2. Attribute 기반 자동 관리

클래스에 `[PoolAddress]` Attribute를 붙이면 자동으로 풀링됩니다.

```csharp
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
    // 풀링 로직은 PoolManager가 자동 처리!
}
```

### 3. AddressableLoader 통합

프리팹 로딩과 해제는 자동으로 AddressableLoader를 통해 처리됩니다.

- 프리팹은 한 번만 로드되어 캐싱됨
- 모든 인스턴스가 반환되면 자동으로 프리팹 해제
- 중복 로드 방지

---

## 기본 사용법

### 1. 클래스에 Attribute 추가

풀링하려는 클래스에 `[PoolAddress]`를 추가합니다.

```csharp
using UnityEngine;
using Core.Pool;

// Pool Container 사용 (권장)
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
    public float speed = 10f;

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
```

### 2. 풀에서 가져오기

`PoolManager.GetFromPool<T>()`로 인스턴스를 가져옵니다.

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using Core.Pool;

public class Gun : MonoBehaviour
{
    public async UniTask FireAsync(CancellationToken ct)
    {
        // 풀에서 총알 가져오기 (없으면 자동 로드)
        var bullet = await PoolManager.GetFromPool<Bullet>(ct);

        if (bullet != null)
        {
            bullet.transform.position = transform.position;
            bullet.transform.rotation = transform.rotation;
        }
    }
}
```

### 3. 풀로 반환하기

사용이 끝나면 `PoolManager.ReturnToPool<T>()`로 반환합니다.

```csharp
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // 충돌 시 풀로 반환
        PoolManager.ReturnToPool(this);
    }

    // 또는 일정 시간 후 자동 반환
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        await UniTask.Delay(3000, cancellationToken: ct); // 3초 후

        PoolManager.ReturnToPool(this);
    }
}
```

### 4. 생명주기 관리 (IPoolable)

풀에서 가져올 때와 반환할 때 호출되는 콜백을 구현할 수 있습니다.

```csharp
using Core.Pool;

[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    public int health;

    // 풀에서 가져올 때 호출 (초기화)
    public void OnGetFromPool()
    {
        health = 100;
        Debug.Log("적 활성화!");
    }

    // 풀로 반환할 때 호출 (정리)
    public void OnReturnToPool()
    {
        health = 0;
        Debug.Log("적 비활성화!");
    }
}
```

**IPoolable 메서드:**
- `OnGetFromPool()`: 풀에서 가져올 때 (활성화 시)
- `OnReturnToPool()`: 풀로 반환할 때 (비활성화 시)

### 5. 프리로드 (게임 시작 시 미리 생성)

자주 사용하는 오브젝트를 미리 풀에 채워둡니다.

```csharp
public class GameInitializer : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 총알 10개 미리 생성
        await PoolManager.PreloadPool<Bullet>(10, ct);

        // 적 5개 미리 생성
        await PoolManager.PreloadPool<Enemy>(5, ct);

        Debug.Log("프리로드 완료!");
    }
}
```

**프리로드 장점:**
- 게임 플레이 중 로딩 없음
- 첫 사용 시 버벅임 제거
- 안정적인 성능 보장

### 6. 사용자 지정 부모 (Parent)

기본적으로 Pool Container를 사용하지만, 원하는 부모를 지정할 수도 있습니다.

```csharp
// 사용자 지정 부모 사용
[PoolAddress("UI/DamageText", "DamageTextParent")]
public class DamageText : MonoBehaviour
{
    // 이 오브젝트는 "DamageTextParent"라는 GameObject 아래에 생성됨
    // 없으면 자동으로 생성됨
}
```

**두 가지 방식:**
```csharp
// 방법 1: Pool Container 사용 (권장)
[PoolAddress("Prefabs/Bullet")]

// 방법 2: 사용자 지정 부모
[PoolAddress("Prefabs/Bullet", "BulletParent")]
```

### 7. DontDestroyOnLoad 옵션

씬이 바뀌어도 풀을 유지하려면 `DontDestroyOnLoad` 옵션을 사용합니다.

```csharp
// 씬 전환 시에도 풀 유지
[PoolAddress("Audio/SFX", dontDestroyOnLoad: true)]
public class SoundEffect : MonoBehaviour
{
    // 이 풀은 씬이 바뀌어도 파괴되지 않음
}

// 사용자 지정 부모 + DontDestroyOnLoad
[PoolAddress("UI/Toast", "ToastParent", dontDestroyOnLoad: true)]
public class ToastMessage : MonoBehaviour
{
}
```

---

## 실전 예제

### 총알 발사 시스템

```csharp
// Bullet.cs
using UnityEngine;
using Core.Pool;
using Cysharp.Threading.Tasks;

[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour, IPoolable
{
    public float speed = 20f;
    public float lifetime = 3f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void OnGetFromPool()
    {
        // 풀에서 가져올 때 초기화
        rb.velocity = Vector3.zero;

        // 3초 후 자동 반환
        AutoReturnAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void OnReturnToPool()
    {
        // 풀로 돌아갈 때 정리
        rb.velocity = Vector3.zero;
    }

    private async UniTaskVoid AutoReturnAsync(CancellationToken ct)
    {
        await UniTask.Delay((int)(lifetime * 1000), cancellationToken: ct);
        PoolManager.ReturnToPool(this);
    }

    public void Fire(Vector3 direction)
    {
        rb.velocity = direction * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 충돌 시 즉시 반환
        PoolManager.ReturnToPool(this);
    }
}

// Gun.cs
using UnityEngine;
using Core.Pool;
using Cysharp.Threading.Tasks;

public class Gun : MonoBehaviour
{
    public Transform firePoint;

    private async UniTaskVoid Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            await FireAsync(this.GetCancellationTokenOnDestroy());
        }
    }

    private async UniTask FireAsync(CancellationToken ct)
    {
        // 풀에서 총알 가져오기
        var bullet = await PoolManager.GetFromPool<Bullet>(ct);

        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;
            bullet.Fire(firePoint.forward);
        }
    }
}
```

### 적 스폰 시스템

```csharp
// Enemy.cs
using UnityEngine;
using Core.Pool;

[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    public int health;
    public int maxHealth = 100;

    public void OnGetFromPool()
    {
        // 활성화 시 체력 초기화
        health = maxHealth;
        gameObject.SetActive(true);
    }

    public void OnReturnToPool()
    {
        // 비활성화
        gameObject.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 사망 시 풀로 반환
        PoolManager.ReturnToPool(this);
    }
}

// EnemySpawner.cs
using UnityEngine;
using Core.Pool;
using Cysharp.Threading.Tasks;

public class EnemySpawner : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 적 5개 프리로드
        await PoolManager.PreloadPool<Enemy>(5, ct);

        // 2초마다 적 스폰
        while (!ct.IsCancellationRequested)
        {
            await SpawnEnemyAsync(ct);
            await UniTask.Delay(2000, cancellationToken: ct);
        }
    }

    private async UniTask SpawnEnemyAsync(CancellationToken ct)
    {
        var enemy = await PoolManager.GetFromPool<Enemy>(ct);

        if (enemy != null)
        {
            // 랜덤 위치에 스폰
            Vector3 randomPos = new Vector3(
                Random.Range(-5f, 5f),
                0,
                Random.Range(-5f, 5f)
            );

            enemy.transform.position = randomPos;
        }
    }
}
```

### 파티클 이펙트

```csharp
using UnityEngine;
using Core.Pool;
using Cysharp.Threading.Tasks;

[PoolAddress("Effects/ExplosionEffect")]
public class ExplosionEffect : MonoBehaviour, IPoolable
{
    private ParticleSystem particles;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
    }

    public void OnGetFromPool()
    {
        // 파티클 재생
        particles.Play();

        // 파티클 종료 후 자동 반환
        AutoReturnAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void OnReturnToPool()
    {
        // 파티클 정지
        particles.Stop();
    }

    private async UniTaskVoid AutoReturnAsync(CancellationToken ct)
    {
        // 파티클 재생 시간만큼 대기
        await UniTask.Delay((int)(particles.main.duration * 1000), cancellationToken: ct);
        PoolManager.ReturnToPool(this);
    }
}

// 사용 예시
public class Explosion : MonoBehaviour
{
    public async UniTask PlayExplosionAsync(Vector3 position, CancellationToken ct)
    {
        var effect = await PoolManager.GetFromPool<ExplosionEffect>(ct);

        if (effect != null)
        {
            effect.transform.position = position;
        }
    }
}
```

### UI 데미지 텍스트

```csharp
using UnityEngine;
using UnityEngine.UI;
using Core.Pool;
using Cysharp.Threading.Tasks;

[PoolAddress("UI/DamageText", "DamageTextParent")]
public class DamageText : MonoBehaviour, IPoolable
{
    public Text text;
    public float moveSpeed = 1f;
    public float lifetime = 1f;

    public void OnGetFromPool()
    {
        // 텍스트 초기화
        text.color = Color.white;

        // 1초 후 자동 반환
        AnimateAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void OnReturnToPool()
    {
        // 정리
    }

    public void SetDamage(int damage)
    {
        text.text = damage.ToString();
    }

    private async UniTaskVoid AnimateAsync(CancellationToken ct)
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            // 위로 이동
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

            // 페이드 아웃
            float alpha = 1f - (elapsed / lifetime);
            text.color = new Color(1, 1, 1, alpha);

            elapsed += Time.deltaTime;
            await UniTask.Yield(ct);
        }

        PoolManager.ReturnToPool(this);
    }
}

// 사용 예시
public class DamageDisplay : MonoBehaviour
{
    public async UniTask ShowDamageAsync(int damage, Vector3 worldPos, CancellationToken ct)
    {
        var damageText = await PoolManager.GetFromPool<DamageText>(ct);

        if (damageText != null)
        {
            // 월드 좌표를 스크린 좌표로 변환
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            damageText.transform.position = screenPos;
            damageText.SetDamage(damage);
        }
    }
}
```

---

## Addressable과 함께 사용하기

Pool 시스템은 AddressableLoader와 통합되어 있습니다. 다음은 두 시스템을 효과적으로 함께 사용하는 예시입니다.

### 예제 1: 동적 스킨 시스템

적 캐릭터는 Pool로 관리하고, 스킨은 Addressable로 동적 로드합니다.

```csharp
using UnityEngine;
using Core.Pool;
using Core.Addressable;
using Cysharp.Threading.Tasks;

[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    public SpriteRenderer spriteRenderer;
    private string currentSkinAddress;

    public void OnGetFromPool()
    {
        // 풀에서 가져올 때 초기화
    }

    public void OnReturnToPool()
    {
        // 스킨 해제
        if (!string.IsNullOrEmpty(currentSkinAddress))
        {
            AddressableLoader.Instance.Release(currentSkinAddress);
            currentSkinAddress = null;
        }
    }

    // 스킨을 동적으로 로드
    public async UniTask SetSkinAsync(string skinAddress, CancellationToken ct)
    {
        // 이전 스킨 해제
        if (!string.IsNullOrEmpty(currentSkinAddress))
        {
            AddressableLoader.Instance.Release(currentSkinAddress);
        }

        // 새 스킨 로드
        var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(skinAddress, ct);

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            currentSkinAddress = skinAddress;
        }
    }
}

// 사용 예시
public class EnemySpawner : MonoBehaviour
{
    private async UniTask SpawnEnemyWithSkinAsync(string skinType, CancellationToken ct)
    {
        // 1. 풀에서 적 가져오기
        var enemy = await PoolManager.GetFromPool<Enemy>(ct);

        if (enemy != null)
        {
            // 2. Addressable로 스킨 로드
            string skinAddress = $"Sprites/EnemySkin_{skinType}";
            await enemy.SetSkinAsync(skinAddress, ct);

            enemy.transform.position = Vector3.zero;
        }
    }
}
```

**핵심 포인트:**
- 적 프리팹: Pool로 관리 (반복 생성/파괴)
- 스킨 텍스처: Addressable로 관리 (동적 로드/해제)
- `OnReturnToPool()`에서 스킨 해제 필수!

### 예제 2: 무기 시스템

총알은 Pool로 관리하고, 무기별 이펙트는 Addressable로 로드합니다.

```csharp
using UnityEngine;
using Core.Pool;
using Core.Addressable;
using Cysharp.Threading.Tasks;

[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour, IPoolable
{
    public TrailRenderer trailRenderer;
    private Material trailMaterial;
    private string trailMaterialAddress;

    public void OnGetFromPool()
    {
        // 초기화
    }

    public void OnReturnToPool()
    {
        // 트레일 머티리얼 해제
        if (!string.IsNullOrEmpty(trailMaterialAddress))
        {
            AddressableLoader.Instance.Release(trailMaterialAddress);
            trailMaterialAddress = null;
            trailMaterial = null;
        }
    }

    // 무기 타입에 따라 트레일 설정
    public async UniTask SetTrailAsync(string weaponType, CancellationToken ct)
    {
        // 트레일 머티리얼 로드
        trailMaterialAddress = $"Materials/Trail_{weaponType}";
        trailMaterial = await AddressableLoader.Instance.LoadAssetAsync<Material>(trailMaterialAddress, ct);

        if (trailMaterial != null && trailRenderer != null)
        {
            trailRenderer.material = trailMaterial;
        }
    }
}

// Gun.cs
public class Gun : MonoBehaviour
{
    public string weaponType = "Laser"; // "Laser", "Fire", "Ice" 등

    private async UniTask FireAsync(CancellationToken ct)
    {
        // 1. 풀에서 총알 가져오기
        var bullet = await PoolManager.GetFromPool<Bullet>(ct);

        if (bullet != null)
        {
            // 2. Addressable로 무기별 이펙트 로드
            await bullet.SetTrailAsync(weaponType, ct);

            bullet.transform.position = transform.position;
            bullet.Fire(transform.forward);
        }
    }
}
```

**핵심 포인트:**
- 총알 프리팹: Pool로 관리
- 트레일 머티리얼: Addressable로 관리
- 무기 타입에 따라 동적으로 다른 이펙트 적용

### 예제 3: 아이템 드롭 시스템

아이템 오브젝트는 Pool로 관리하고, 아이콘은 Addressable로 로드합니다.

```csharp
using UnityEngine;
using UnityEngine.UI;
using Core.Pool;
using Core.Addressable;
using Cysharp.Threading.Tasks;

[PoolAddress("Prefabs/DroppedItem")]
public class DroppedItem : MonoBehaviour, IPoolable
{
    public SpriteRenderer iconRenderer;
    private string iconAddress;

    public void OnGetFromPool()
    {
        // 초기화
    }

    public void OnReturnToPool()
    {
        // 아이콘 해제
        if (!string.IsNullOrEmpty(iconAddress))
        {
            AddressableLoader.Instance.Release(iconAddress);
            iconAddress = null;
        }
    }

    public async UniTask SetItemAsync(int itemId, CancellationToken ct)
    {
        // 아이템 아이콘 로드
        iconAddress = $"Icons/Item_{itemId}";
        var icon = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(iconAddress, ct);

        if (icon != null && iconRenderer != null)
        {
            iconRenderer.sprite = icon;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어가 아이템 획득 시 풀로 반환
            PoolManager.ReturnToPool(this);
        }
    }
}

// ItemDropper.cs
public class ItemDropper : MonoBehaviour
{
    public async UniTask DropItemAsync(int itemId, Vector3 position, CancellationToken ct)
    {
        // 1. 풀에서 아이템 오브젝트 가져오기
        var item = await PoolManager.GetFromPool<DroppedItem>(ct);

        if (item != null)
        {
            // 2. Addressable로 아이템 아이콘 로드
            await item.SetItemAsync(itemId, ct);

            item.transform.position = position;
        }
    }
}
```

### 예제 4: 사운드 이펙트 + 파티클

파티클 오브젝트는 Pool로 관리하고, 사운드는 Addressable로 로드합니다.

```csharp
using UnityEngine;
using Core.Pool;
using Core.Addressable;
using Cysharp.Threading.Tasks;

[PoolAddress("Effects/HitEffect")]
public class HitEffect : MonoBehaviour, IPoolable
{
    private ParticleSystem particles;
    private AudioSource audioSource;
    private string soundAddress;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
    }

    public void OnGetFromPool()
    {
        particles.Play();
        AutoReturnAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public void OnReturnToPool()
    {
        particles.Stop();

        // 사운드 해제
        if (!string.IsNullOrEmpty(soundAddress))
        {
            AddressableLoader.Instance.Release(soundAddress);
            soundAddress = null;
        }
    }

    public async UniTask PlayWithSoundAsync(string soundType, CancellationToken ct)
    {
        // 사운드 로드
        soundAddress = $"Audio/SFX_{soundType}";
        var clip = await AddressableLoader.Instance.LoadAssetAsync<AudioClip>(soundAddress, ct);

        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private async UniTaskVoid AutoReturnAsync(CancellationToken ct)
    {
        await UniTask.Delay((int)(particles.main.duration * 1000), cancellationToken: ct);
        PoolManager.ReturnToPool(this);
    }
}

// 사용 예시
public class Combat : MonoBehaviour
{
    public async UniTask PlayHitEffectAsync(Vector3 position, string soundType, CancellationToken ct)
    {
        // 1. 풀에서 이펙트 가져오기
        var effect = await PoolManager.GetFromPool<HitEffect>(ct);

        if (effect != null)
        {
            effect.transform.position = position;

            // 2. Addressable로 사운드 로드 및 재생
            await effect.PlayWithSoundAsync(soundType, ct);
        }
    }
}
```

### 예제 5: 프리로드 전략

게임 시작 시 Pool과 Addressable을 함께 프리로드합니다.

```csharp
using UnityEngine;
using Core.Pool;
using Core.Addressable;
using Cysharp.Threading.Tasks;

public class GameLoader : MonoBehaviour
{
    private async UniTaskVoid Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // === 1단계: 공용 리소스 프리로드 (Addressable) ===
        Debug.Log("공용 리소스 로딩 중...");

        await AddressableLoader.Instance.PreloadAsync(new[]
        {
            "UI/MainMenu",
            "Audio/BGM_Title",
            "Materials/Common"
        }, ct);

        // === 2단계: 게임 오브젝트 프리로드 (Pool) ===
        Debug.Log("게임 오브젝트 프리로드 중...");

        // 총알 20개 미리 생성
        await PoolManager.PreloadPool<Bullet>(20, ct);

        // 적 10개 미리 생성
        await PoolManager.PreloadPool<Enemy>(10, ct);

        // 이펙트 5개 미리 생성
        await PoolManager.PreloadPool<HitEffect>(5, ct);

        // === 3단계: 게임 시작 ===
        Debug.Log("로딩 완료!");
    }

    private void OnApplicationQuit()
    {
        // 모든 리소스 정리
        AddressableLoader.Instance.ReleaseAll();
        PoolManager.ClearAllPools();
    }
}
```

### 사용 패턴 요약

| 리소스 유형 | 시스템 | 이유 |
|-----------|--------|------|
| **자주 생성/파괴되는 프리팹** | Pool | 성능 최적화 |
| **동적으로 바뀌는 텍스처/머티리얼** | Addressable | 메모리 관리 |
| **사운드 클립** | Addressable | 선택적 로딩 |
| **공용 UI 리소스** | Addressable | 메모리 효율 |
| **적, 총알, 이펙트 등** | Pool | GC 최소화 |

**핵심 원칙:**
- 프리팹은 Pool로 관리
- 프리팹이 사용하는 에셋(스프라이트, 머티리얼, 사운드)은 Addressable로 관리
- `OnReturnToPool()`에서 Addressable 리소스 해제 필수
- 프리로드로 로딩 시간 최소화

---

## 고급 기능

### 풀 정리하기

```csharp
// 특정 타입의 풀 비우기
PoolManager.ClearPoolType<Bullet>();

// 모든 풀 비우기 (씬 전환 시 등)
PoolManager.ClearAllPools();
```

### 디버깅

```csharp
// 풀 상태 확인
PoolManager.PrintDebugInfo();
```

**출력 예시:**
```
=== [PoolManager] 상태 ===
초기화 여부: True
Pool 개수: 3
Parent 캐시: 2

[타입별 Pool]
- Bullet
- Enemy
- ExplosionEffect

[Parent 캐시]
- 'DamageTextParent' → DamageTextParent
- 'BulletParent' → BulletParent
===========================
```

---

## 주의사항

### ⚠️ 반드시 PoolAddress Attribute 추가

```csharp
// ❌ 나쁜 예: Attribute 없음
public class Bullet : MonoBehaviour
{
}

// GetFromPool 호출 시 오류 발생!
var bullet = await PoolManager.GetFromPool<Bullet>(ct);

// ✅ 좋은 예: Attribute 추가
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
}
```

### ⚠️ Get과 Return 짝 맞추기

```csharp
// ❌ 나쁜 예: Return을 호출하지 않음
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
// Return 안 함 → 풀에 돌아가지 않아 메모리 낭비!

// ✅ 좋은 예: 사용 후 반드시 Return
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
// 사용...
PoolManager.ReturnToPool(bullet);
```

### ⚠️ OnReturnToPool에서 Addressable 리소스 해제

```csharp
// ❌ 나쁜 예: Addressable 리소스 해제 안 함
[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    private string skinAddress;

    public async UniTask SetSkinAsync(string address, CancellationToken ct)
    {
        skinAddress = address;
        var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(address, ct);
    }

    public void OnReturnToPool()
    {
        // skinAddress 해제 안 함 → 메모리 누수!
    }
}

// ✅ 좋은 예: Addressable 리소스 해제
[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    private string skinAddress;

    public async UniTask SetSkinAsync(string address, CancellationToken ct)
    {
        skinAddress = address;
        var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(address, ct);
    }

    public void OnReturnToPool()
    {
        // Addressable 리소스 해제
        if (!string.IsNullOrEmpty(skinAddress))
        {
            AddressableLoader.Instance.Release(skinAddress);
            skinAddress = null;
        }
    }
}
```

### ⚠️ null 체크 필수

```csharp
// ❌ 나쁜 예: null 체크 없음
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
bullet.transform.position = Vector3.zero; // NullReferenceException 가능!

// ✅ 좋은 예: null 체크
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
if (bullet != null)
{
    bullet.transform.position = Vector3.zero;
}
```

### ⚠️ Destroy 대신 ReturnToPool 사용

```csharp
// ❌ 나쁜 예: Destroy 사용
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
Destroy(bullet.gameObject); // 풀링 의미 없음!

// ✅ 좋은 예: ReturnToPool 사용
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
PoolManager.ReturnToPool(bullet);
```

### ⚠️ IPoolable은 선택사항

IPoolable 인터페이스는 필수가 아닙니다. 생명주기 관리가 필요할 때만 구현하세요.

```csharp
// IPoolable 없이도 사용 가능
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
    // 풀링은 정상 작동함
}

// 초기화/정리가 필요하면 IPoolable 구현
[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    public void OnGetFromPool() { /* 초기화 */ }
    public void OnReturnToPool() { /* 정리 */ }
}
```

### ⚠️ Address 경로 주의

```csharp
// ❌ 잘못된 경로: 백슬래시 사용
[PoolAddress("Prefabs\\Bullet")] // 오류 발생!

// ✅ 올바른 경로: 슬래시 사용
[PoolAddress("Prefabs/Bullet")]
```

---

## FAQ

### Q1. Object Pooling은 언제 사용하나요?

A. 다음과 같은 경우에 사용합니다:
- ✅ 총알, 적, 이펙트 등 **반복 생성/파괴**되는 오브젝트
- ✅ **1초에 여러 번** 생성되는 오브젝트
- ✅ **GC Spike**를 줄이고 싶을 때
- ✅ 모바일 게임 등 **성능이 중요**한 경우

**사용하지 않아도 되는 경우:**
- ❌ 한 번만 생성되는 오브젝트 (플레이어, 메인 UI 등)
- ❌ 드물게 생성되는 오브젝트
- ❌ 복잡한 초기화가 필요한 오브젝트

### Q2. 프리로드는 필수인가요?

A. 아니오, 선택사항입니다.

```csharp
// 프리로드 없이도 동작
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
// → 풀이 비어있으면 자동으로 로드됨

// 프리로드하면 더 부드러움
await PoolManager.PreloadPool<Bullet>(10, ct);
var bullet = await PoolManager.GetFromPool<Bullet>(ct);
// → 즉시 가져옴 (로딩 없음)
```

**권장:** 게임 시작 시 자주 사용하는 오브젝트 프리로드

### Q3. 풀 크기는 어떻게 설정하나요?

A. 기본값은 타입당 10개입니다. 변경하려면:

```csharp
// ObjectPool을 직접 사용하는 경우 (고급)
var pool = ObjectPool<Component>.CreateForAddressable(defaultMaxSize: 20);
```

**기본 동작:**
- 풀 크기 초과 시 추가 인스턴스는 파괴됨
- 대부분의 경우 기본값(10개)으로 충분

### Q4. 씬 전환 시 풀은 어떻게 되나요?

A. 기본적으로 씬이 바뀌면 풀도 파괴됩니다.

```csharp
// 씬 전환 시 파괴됨 (기본값)
[PoolAddress("Prefabs/Bullet")]

// 씬 전환 시에도 유지됨
[PoolAddress("Prefabs/Bullet", dontDestroyOnLoad: true)]
```

**권장:** 대부분의 경우 기본값 사용, 글로벌 오브젝트만 DontDestroyOnLoad

### Q5. Pool과 Addressable을 함께 쓸 때 주의할 점은?

A. `OnReturnToPool()`에서 반드시 Addressable 리소스를 해제하세요!

```csharp
public void OnReturnToPool()
{
    // Addressable로 로드한 리소스 해제
    if (!string.IsNullOrEmpty(skinAddress))
    {
        AddressableLoader.Instance.Release(skinAddress);
        skinAddress = null;
    }
}
```

해제하지 않으면 메모리 누수가 발생합니다.

### Q6. 이미 활성화된 오브젝트를 다시 GetFromPool하면?

A. 풀에 없으면 새로 생성됩니다. 문제없습니다.

```csharp
var bullet1 = await PoolManager.GetFromPool<Bullet>(ct); // 풀에서 가져옴
var bullet2 = await PoolManager.GetFromPool<Bullet>(ct); // 새로 생성됨

// bullet1과 bullet2는 다른 인스턴스
```

### Q7. ReturnToPool을 여러 번 호출하면?

A. 두 번째 호출부터는 무시됩니다. 안전합니다.

---

## 요약

**Object Pool 사용 3단계:**

1. **클래스에 Attribute 추가** (`[PoolAddress("경로")]`)
2. **GetFromPool으로 가져오기**
3. **ReturnToPool으로 반환**

```csharp
// 1. Attribute 추가
[PoolAddress("Prefabs/Bullet")]
public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // 3. 사용 후 반환
        PoolManager.ReturnToPool(this);
    }
}

// 2. 가져오기
public class Gun : MonoBehaviour
{
    private async UniTask FireAsync(CancellationToken ct)
    {
        var bullet = await PoolManager.GetFromPool<Bullet>(ct);

        if (bullet != null)
        {
            bullet.transform.position = transform.position;
        }
    }
}
```

**Addressable과 함께 사용 시:**
```csharp
[PoolAddress("Prefabs/Enemy")]
public class Enemy : MonoBehaviour, IPoolable
{
    private string skinAddress;

    public async UniTask SetSkinAsync(string address, CancellationToken ct)
    {
        skinAddress = address;
        var sprite = await AddressableLoader.Instance.LoadAssetAsync<Sprite>(address, ct);
        // 스프라이트 적용...
    }

    public void OnReturnToPool()
    {
        // Addressable 리소스 해제 필수!
        if (!string.IsNullOrEmpty(skinAddress))
        {
            AddressableLoader.Instance.Release(skinAddress);
            skinAddress = null;
        }
    }
}
```

**핵심 원칙:**
- Get과 Return은 항상 쌍으로
- Destroy 대신 ReturnToPool 사용
- 자주 생성/파괴되는 오브젝트에만 사용
- 프리로드로 성능 최적화
- **Addressable 리소스는 OnReturnToPool에서 해제**

**추가 정보:**
- 소스 코드: `Assets/Scripts/Core/Pool/PoolManager.cs`
- ObjectPool 구현: `Assets/Scripts/Core/Pool/ObjectPool.cs`
- Attribute 정의: `Assets/Scripts/Core/Pool/PoolAddressAttribute.cs`
- Addressable 통합: `Assets/Scripts/Core/Addressable/AddressableLoader.cs`
