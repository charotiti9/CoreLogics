/// <summary>
/// 싱글톤 패턴 구현
///
/// 사용 가이드:
/// - LazySingleton<T>: 처음 사용 시 생성 (메모리 효율적, 무거운 초기화에 적합)
/// - EagerSingleton<T>: 타입 로드 시 즉시 생성 (성능 우수, 핵심 매니저에 적합)
/// </summary>



/// <summary>
/// Eager 초기화 싱글톤 베이스 클래스
/// 타입이 로드될 때 즉시 인스턴스가 생성됨
/// 게임 시작 시 반드시 필요한 핵심 매니저에 적합
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
public abstract class EagerSingleton<T> where T : class, new()
{
    private static readonly T _instance;

    /// <summary>
    /// 정적 생성자는 CLR이 타입 최초 접근 시 단 한 번만 실행함을 보장 (내부적으로 lock 사용)
    /// = 멀티스레드 환경에서도 lock 없이 안전
    /// </summary>
    static EagerSingleton()
    {
        _instance = new T();
        (_instance as EagerSingleton<T>)?.Initialize();
    }

    public static T Instance => _instance;

    /// <summary>
    /// 싱글톤 인스턴스가 생성되어 있는지 확인
    /// Eager 싱글톤은 타입 로드 시 즉시 생성되므로 항상 true
    /// </summary>
    /// <returns>항상 true</returns>
    public static bool IsAlive() => _instance != null;

    /// <summary>
    /// 파생 클래스에서 직접 생성하지 못하도록 protected
    /// </summary>
    protected EagerSingleton()
    {
    }

    /// <summary>
    /// 싱글톤 인스턴스 초기화
    /// 파생 클래스에서 오버라이드하여 초기화 로직 구현
    /// </summary>
    protected virtual void Initialize()
    {
    }

    // Eager 싱글톤은 한번 생성되면 해제할 수 없음
}



/// <summary>
/// Lazy 초기화 싱글톤 베이스 클래스
/// MonoBehaviour를 사용하지 않는 매니저나 시스템에 사용
/// 처음 Instance에 접근할 때 생성됨
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
public abstract class LazySingleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            // lock 건 이후에도 _instance가 생성되었을 수도 있으니
            // Double-checked locking 패턴으로 스레드 세이프 보장
            if (_instance == null)
            {
                lock (_lock)
                {
                    // 초기화 메서드 호출
                    if (_instance == null)
                    {
                        _instance = new T();
                        (_instance as LazySingleton<T>)?.Initialize();
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 싱글톤 인스턴스가 생성되어 있는지 확인
    /// </summary>
    /// <returns>인스턴스가 존재하면 true, 아니면 false</returns>
    public static bool IsAlive() => _instance != null;

    /// <summary>
    /// 파생 클래스에서 직접 생성하지 못하도록 protected
    /// </summary>
    protected LazySingleton()
    {
    }

    /// <summary>
    /// 싱글톤 인스턴스 초기화
    /// 파생 클래스에서 오버라이드하여 초기화 로직 구현
    /// </summary>
    protected virtual void Initialize()
    {
    }

    /// <summary>
    /// 싱글톤 인스턴스 해제
    /// 주로 테스트나 씬 전환 시 사용
    /// </summary>
    public static void DestroyInstance()
    {
        lock (_lock)
        {
            _instance = null;
        }
    }
}
