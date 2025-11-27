using UnityEngine;
using Cysharp.Threading.Tasks;
using Common.UI;

namespace Core.Game.States
{
    /// <summary>
    /// 타이틀 화면 상태 (예시)
    /// 타이틀 UI를 표시하고 사용자 입력을 대기합니다.
    /// </summary>
    public class TitleState : IState<GameContext>
    {
        public void Enter(GameContext context)
        {
            Debug.Log("[TitleState] 타이틀 화면 진입");

            // UIManager를 통해 타이틀 UI 표시
            // 주의: TitleUI 클래스가 실제로 존재해야 합니다
            // ShowTitleUIAsync(context).Forget();
        }

        /// <summary>
        /// 타이틀 UI 표시 (예시)
        /// </summary>
        private async UniTaskVoid ShowTitleUIAsync(GameContext context)
        {
            // 예시: TitleUI 표시 (UIManager는 싱글톤이므로 Instance로 접근)
            // var titleUI = await UIManager.Instance.ShowAsync<TitleUI>(
            //     UILayer.Overlay,
            //     data: null,
            //     useDim: false,
            //     ct: context.CancellationToken
            // );

            // UI 버튼 이벤트 구독 예시:
            // titleUI.OnStartButtonClicked += () =>
            // {
            //     // 게임 시작 - GameplayState로 전환
            //     context.StateMachine.ChangeState(new GameplayState());
            // };

            Debug.Log("[TitleState] 타이틀 UI 표시 완료 (예시)");
        }

        /// <summary>
        /// 매 프레임 호출 (GameFlowManager → StateMachine → 현재 State 순서로 자동 호출)
        /// </summary>
        public void Update(GameContext context, float deltaTime)
        {
            // 타이틀 화면 업데이트 로직
            // 예: 입력 처리, 애니메이션 업데이트 등
        }

        public void Exit(GameContext context)
        {
            Debug.Log("[TitleState] 타이틀 화면 종료");

            // 타이틀 UI 숨김 (UIManager는 싱글톤이므로 Instance로 접근)
            // UIManager.Instance.Hide<TitleUI>();
        }
    }
}
