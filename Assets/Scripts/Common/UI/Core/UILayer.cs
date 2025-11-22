namespace Common.UI
{
    /// <summary>
    /// UI 레이어 정의
    /// 각 레이어는 sortingOrder를 기반으로 깊이를 관리합니다.
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 배경 UI (sortingOrder: 0)
        /// - 게임 배경이나 전체 화면 베이스 UI
        /// - 가장 낮은 깊이, 상호작용 최소화
        /// </summary>
        Background = 0,

        /// <summary>
        /// 게임플레이 HUD (sortingOrder: 100)
        /// - 게임플레이 중 항상 표시되는 핵심 정보
        /// - 체력바, 미니맵, 스킬 쿨타임 등
        /// </summary>
        HUD = 100,

        /// <summary>
        /// 일반 오버레이 UI (sortingOrder: 200)
        /// - 게임의 주요 화면 및 일반적인 UI
        /// - 메인 메뉴, 인벤토리, 상점 등
        /// </summary>
        Overlay = 200,

        /// <summary>
        /// 팝업 (sortingOrder: 300)
        /// - 일시적으로 표시되는 팝업 창
        /// - 확인/취소 다이얼로그, 보상 팝업 등
        /// </summary>
        PopUp = 300,

        /// <summary>
        /// 시스템 UI (sortingOrder: 400)
        /// - 시스템 레벨의 알림 및 피드백 UI
        /// - 토스트 메시지, 푸시 알림 등
        /// </summary>
        System = 400,

        /// <summary>
        /// 전환 효과 UI (sortingOrder: 500)
        /// - 화면 전환이나 로딩 등 모든 UI를 가리는 UI
        /// - 로딩 화면, 씬 전환 페이드 등
        /// </summary>
        Transition = 500
    }
}
