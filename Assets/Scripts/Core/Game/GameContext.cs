using System.Threading;
using Common.UI;

namespace Core.Game
{
    /// <summary>
    /// 게임 전역 컨텍스트
    /// StateMachine이 조작하는 게임 상태 데이터를 관리합니다.
    ///
    /// 주의: 싱글톤 매니저(UIManager 등)는 Context에 저장하지 않습니다.
    /// 싱글톤은 Instance 프로퍼티로 직접 접근하세요.
    /// </summary>
    public class GameContext
    {
        /// <summary>
        /// StateMachine 참조 (상태에서 자동 전환을 위해 필요)
        /// </summary>
        public StateMachine<GameContext> StateMachine { get; set; }

        /// <summary>
        /// 전역 CancellationToken (게임 종료 시 취소)
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized { get; set; }

        // 싱글톤이 아닌 매니저만 추가하세요
        // 예: 플레이어 데이터, 게임 설정 등
        // public PlayerData PlayerData { get; set; }
        // public GameSettings Settings { get; set; }
    }
}
