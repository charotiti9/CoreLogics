using System;

/// <summary>
/// 일반 C# 클래스를 위한 싱글톤 베이스 클래스
/// MonoBehaviour를 사용하지 않는 매니저나 시스템에 사용
/// </summary>
/// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
public abstract class Singleton<T> where T : class, new()
{
    private static T _instance;
    private static readonly object _lock = new object();

    /// <summary>
    /// 싱글톤 인스턴스에 접근
    /// </summary>
    public static T Instance
    {
        get
        {
            // Double-checked locking 패턴으로 스레드 세이프 보장
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();

                        // 초기화 메서드 호출
                        if (_instance is Singleton<T> singleton)
                        {
                            singleton.Initialize();
                        }
                    }
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// 생성자 - 파생 클래스에서 직접 생성하지 못하도록 protected
    /// </summary>
    protected Singleton()
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
