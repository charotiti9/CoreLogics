namespace Common.UI
{
    /// <summary>
    /// UI 레이어 정의
    /// 숫자가 클수록 상위 레이어입니다. (0이 가장 아래, 5가 가장 위)
    /// MainCanvas 하위에 레이어별 GameObject가 이 순서대로 배치됩니다.
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 배경 UI
        /// - 게임 배경이나 전체 화면 베이스 UI
        /// - 가장 낮은 깊이, 상호작용 최소화
        /// </summary>
        Background = 0,

        /// <summary>
        /// 게임플레이 HUD
        /// - 게임플레이 중 항상 표시되는 핵심 정보
        /// - 체력바, 미니맵, 스킬 쿨타임 등
        /// </summary>
        HUD = 1,

        /// <summary>
        /// 일반 오버레이 UI
        /// - 게임의 주요 화면 및 일반적인 UI
        /// - 메인 메뉴, 인벤토리, 상점 등
        /// </summary>
        Overlay = 2,

        /// <summary>
        /// 팝업
        /// - 일시적으로 표시되는 팝업 창
        /// - 확인/취소 다이얼로그, 보상 팝업 등
        /// </summary>
        PopUp = 3,

        /// <summary>
        /// 시스템 UI
        /// - 시스템 레벨의 알림 및 피드백 UI
        /// - 토스트 메시지, 푸시 알림 등
        /// </summary>
        System = 4,

        /// <summary>
        /// 전환 효과 UI
        /// - 화면 전환이나 로딩 등 모든 UI를 가리는 UI
        /// - 로딩 화면, 씬 전환 페이드 등
        /// </summary>
        Transition = 5
    }
}
