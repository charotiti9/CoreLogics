using UnityEngine;

/// <summary>
/// 중앙 입력 관리자 (수동 작성 부분)
/// 자동 생성된 InputManager.Generated.cs와 결합되는 partial class
/// </summary>
public partial class InputManager : EagerMonoSingleton<InputManager>
{
    // InputSystem_Actions 인스턴스
    private InputSystem_Actions inputActions;

    /// <summary>
    /// DontDestroyOnLoad 적용 여부
    /// </summary>
    protected override bool IsPersistent => true;

    /// <summary>
    /// 초기화
    /// </summary>
    protected override void Initialize()
    {
        // InputSystem_Actions 인스턴스 생성
        inputActions = new InputSystem_Actions();

        // 자동 생성된 바인딩 메서드 호출
        BindAllActions();

        // 기본적으로 Player 입력 활성화
        EnablePlayerInput();
    }

    #region Action Map 제어

    /// <summary>
    /// Player 입력 활성화
    /// </summary>
    public void EnablePlayerInput()
    {
        inputActions.Player.Enable();
    }

    /// <summary>
    /// Player 입력 비활성화
    /// </summary>
    public void DisablePlayerInput()
    {
        inputActions.Player.Disable();
    }

    /// <summary>
    /// UI 입력 활성화
    /// </summary>
    public void EnableUIInput()
    {
        inputActions.UI.Enable();
    }

    /// <summary>
    /// UI 입력 비활성화
    /// </summary>
    public void DisableUIInput()
    {
        inputActions.UI.Disable();
    }

    /// <summary>
    /// 모든 입력 활성화
    /// </summary>
    public void EnableAllInput()
    {
        inputActions.Enable();
    }

    /// <summary>
    /// 모든 입력 비활성화
    /// </summary>
    public void DisableAllInput()
    {
        inputActions.Disable();
    }

    #endregion

    /// <summary>
    /// 정리
    /// </summary>
    protected override void OnDestroy()
    {
        // 모든 액션 비활성화 및 정리
        inputActions?.Disable();
        inputActions?.Dispose();

        base.OnDestroy();
    }
}
