using UnityEngine;

/// <summary>
/// MonoBehaviour를 위한 싱글톤 베이스 클래스
/// 씬에 존재해야 하는 매니저에만 사용 (최소화 권장)
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 MonoBehaviour 타입</typeparam>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool isApplicationQuitting = false;

    /// <summary>
    /// 싱글톤 인스턴스에 접근
    /// </summary>
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
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    // 씬에도 없으면 새로 생성
                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"[Singleton] {typeof(T).Name}";
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// DontDestroyOnLoad 적용 여부 (기본값: true)
    /// 파생 클래스에서 오버라이드하여 변경 가능
    /// </summary>
    protected virtual bool IsPersistent => true;

    /// <summary>
    /// Unity Awake - 싱글톤 설정
    /// </summary>
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
    /// 초기화 메서드 - 파생 클래스에서 오버라이드
    /// </summary>
    protected virtual void Initialize()
    {
    }

    /// <summary>
    /// 애플리케이션 종료 시 호출
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    /// <summary>
    /// 파괴 시 호출
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 자신이 싱글톤 인스턴스인 경우에만 해제
        if (_instance == this)
        {
            isApplicationQuitting = true;
        }
    }
}
