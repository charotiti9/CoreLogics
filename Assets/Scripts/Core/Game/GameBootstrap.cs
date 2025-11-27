using System.Threading;
using UnityEngine;
using Core.Game.States;

namespace Core.Game
{
    /// <summary>
    /// 게임 부트스트랩
    /// 게임 진입점으로, StateMachine을 생성하고 InitializeState로 시작합니다.
    ///
    /// 사용법:
    /// 1. 씬에 빈 GameObject를 생성하고 이 컴포넌트를 추가하세요.
    /// 2. 게임 시작 시 자동으로 초기화가 진행됩니다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private StateMachine<GameContext> stateMachine;
        private GameContext context;
        private CancellationTokenSource cts;

        private void Awake()
        {
            // DontDestroyOnLoad 설정
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameBootstrap] 게임 부트스트랩 시작");

            // CancellationTokenSource 생성
            cts = new CancellationTokenSource();

            // GameContext 생성
            context = new GameContext
            {
                CancellationToken = cts.Token
            };

            // StateMachine 생성 (IUpdatable로 GameFlowManager에 자동 등록됨)
            stateMachine = new StateMachine<GameContext>(context);

            // Context에 StateMachine 참조 저장 (상태에서 자동 전환 시 필요)
            context.StateMachine = stateMachine;

            // InitializeState로 시작
            stateMachine.ChangeState(new InitializeState());

            Debug.Log("[GameBootstrap] StateMachine 시작됨 (InitializeState)");
        }

        private void OnDestroy()
        {
            Debug.Log("[GameBootstrap] 게임 부트스트랩 종료");

            // CancellationTokenSource 취소
            cts?.Cancel();
            cts?.Dispose();

            // StateMachine 정리 (GameFlowManager에서 자동 등록 해제됨)
            stateMachine?.Dispose();
        }
    }
}
