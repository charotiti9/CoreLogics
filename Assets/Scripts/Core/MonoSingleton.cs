using UnityEngine;

/// <summary>
/// MonoBehaviour 싱글톤 패턴 구현
///
/// 사용 가이드:
/// - LazyMonoSingleton<T>: Instance 접근 시 자동 생성 (편리함, 간단한 매니저에 적합)
/// - EagerMonoSingleton<T>: 씬에 반드시 미리 배치 (명시적, Inspector 설정 필요한 경우 적합)
/// </summary>



/// <summary>
/// Eager 초기화 MonoBehaviour 싱글톤 베이스 클래스
/// 씬에 반드시 미리 GameObject를 배치해야 함
/// Inspector 설정이 필요하거나 명시적인 관리가 필요한 경우 사용
/// 하이브리드 방식: Awake 전 접근 시에만 FindFirstObjectByType 호출, 이후는 단순 반환
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 MonoBehaviour 타입</typeparam>
public abstract class EagerMonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static bool isInitialized = false;
    private static bool isApplicationQuitting = false;

    public static T Instance
    {
        get
        {
            // 애플리케이션 종료 중에는 null 반환
            if (isApplicationQuitting)
            {
                return null;
            }

            // Awake 전 접근 시에만 한 번 찾기
            if (_instance == null && !isInitialized)
            {
                _instance = FindFirstObjectByType<T>();
            }

            // 여전히 null이면 경고
            if (_instance == null)
            {
                Debug.LogError($"[EagerMonoSingleton] {typeof(T).Name}이(가) 씬에 존재하지 않습니다. " +
                               $"씬에 GameObject를 배치하고 {typeof(T).Name} 컴포넌트를 추가해주세요.");
            }

            return _instance;
        }
    }

    /// <summary>
    /// 싱글톤 인스턴스가 생성되어 있고 파괴되지 않았는지 확인
    /// </summary>
    /// <returns>인스턴스가 존재하고 살아있으면 true, 아니면 false</returns>
    public static bool IsAlive() => _instance != null && !isApplicationQuitting;

    /// <summary>
    /// DontDestroyOnLoad 적용 여부 (기본값: true)
    /// 파생 클래스에서 오버라이드하여 변경 가능
    /// </summary>
    protected virtual bool IsPersistent => true;

    protected virtual void Awake()
    {
        // 이미 인스턴스가 존재하는 경우
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[EagerMonoSingleton] {typeof(T).Name}이(가) 씬에 중복으로 존재합니다. " +
                           $"중복 인스턴스를 제거합니다.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        isInitialized = true;

        // DontDestroyOnLoad 설정
        if (IsPersistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        // 초기화
        Initialize();
    }

    /// <summary>
    /// 초기화 메서드 파생 클래스에서 오버라이드하여 변경 가능
    /// </summary>
    protected virtual void Initialize()
    {
    }


    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        // 자신이 싱글톤 인스턴스인 경우에만 해제
        if (_instance == this)
        {
            isApplicationQuitting = true;
            isInitialized = false;
        }
    }
}



/// <summary>
/// Lazy 초기화 MonoBehaviour 싱글톤 베이스 클래스
/// Instance 접근 시 씬에서 찾거나 자동으로 생성
/// 씬에 미리 배치하지 않아도 동작하는 편리한 방식
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 MonoBehaviour 타입</typeparam>
public abstract class LazyMonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool isApplicationQuitting = false;

    public static T Instance
    {
        get
        {
            // 애플리케이션 종료 중에는 null 반환 (워닝 방지)
            if (isApplicationQuitting)
            {
                return null;
            }

            lock (_lock)
            {
                // 기존 인스턴스가 없으면 씬에서 찾기
                _instance ??= FindFirstObjectByType<T>();

                // 씬에도 없으면 새로 생성
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                    _instance = singletonObject.AddComponent<T>();
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// 싱글톤 인스턴스가 생성되어 있고 파괴되지 않았는지 확인
    /// </summary>
    /// <returns>인스턴스가 존재하고 살아있으면 true, 아니면 false</returns>
    public static bool IsAlive() => _instance != null && !isApplicationQuitting;

    /// <summary>
    /// DontDestroyOnLoad 적용 여부 (기본값: true)
    /// 파생 클래스에서 오버라이드하여 변경 가능
    /// </summary>
    protected virtual bool IsPersistent => true;

    protected virtual void Awake()
    {
        // 이미 인스턴스가 존재하는 경우
        if (_instance != null && _instance != this)
        {
            // 중복 인스턴스 제거
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        // DontDestroyOnLoad 설정
        if (IsPersistent)
        {
            DontDestroyOnLoad(gameObject);
        }

        // 초기화
        Initialize();
    }

    /// <summary>
    /// 초기화 메서드 파생 클래스에서 오버라이드하여 변경 가능
    /// </summary>
    protected virtual void Initialize()
    {
    }

    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        // 자신이 싱글톤 인스턴스인 경우에만 해제
        if (_instance == this)
        {
            isApplicationQuitting = true;
        }
    }
}