using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Common.UI;

namespace Core.Game.States
{
    /// <summary>
    /// 게임 초기화 상태
    /// 모든 매니저를 초기화하고 타이틀 화면으로 전환합니다.
    /// </summary>
    public class InitializeState : IState<GameContext>
    {
        public void Enter(GameContext context)
        {
            // 비동기 초기화 시작 (Fire-and-forget)
            InitializeAsync(context).Forget();
        }

        /// <summary>
        /// 비동기 초기화 로직
        /// 매니저 초기화 대기 및 기타 초기화를 수행합니다.
        /// 참고: UIManager, AudioManager 등은 EagerMonoSingleton을 사용하므로 씬에 미리 배치되어야 합니다.
        /// </summary>
        private async UniTaskVoid InitializeAsync(GameContext context)
        {
            try
            {
                // 1. UIManager는 EagerMonoSingleton으로 씬에 배치되어 Awake에서 자동 초기화됨

                // 2. 다른 싱글톤 매니저들도 EagerMonoSingleton을 사용하므로 씬에 배치되어야 함
                // AudioManager, PoolManager 등은 Awake에서 자동 초기화됨

                // 3. CSV 데이터 로딩 (CSVManager.Initialize가 내부적으로 LoadAllTablesAsync 호출)
                Debug.Log("[InitializeState] CSV 데이터 로딩 중...");
                await CSVManager.Instance.Initialize(context.CancellationToken);
                Debug.Log("[InitializeState] CSV 데이터 로딩 완료");

                // 4. Localization 초기화
                Debug.Log("[InitializeState] Localization 초기화 중...");
                LocalizationManager.Instance.Initialize();
                Debug.Log("[InitializeState] Localization 초기화 완료");

                // 5. 초기화 완료 표시
                context.IsInitialized = true;

                // 6. 다음 상태로 자동 전환 (TitleState)
                // 주의: 실제 프로젝트에서는 TitleState를 구현해야 합니다
                // context.StateMachine.ChangeState(new TitleState());

                Debug.Log("[InitializeState] 초기화 완료. 다음 상태로 전환하세요.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[InitializeState] 초기화가 취소되었습니다.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InitializeState] 초기화 실패: {ex.Message}\n{ex.StackTrace}");

                // 초기화 실패 시 처리
                // 예: 에러 UI 표시, 재시도, 게임 종료 등
            }
        }

        public void Update(GameContext context, float deltaTime)
        {
            // 초기화 진행 중... (필요시 로딩 UI 업데이트)
        }

        public void Exit(GameContext context)
        {
            Debug.Log("[InitializeState] 상태 종료");
        }
    }
}
