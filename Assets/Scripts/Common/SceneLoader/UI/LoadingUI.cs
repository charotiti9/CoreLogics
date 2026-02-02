using System.Threading;
using Cysharp.Threading.Tasks;
using Common.UI;

namespace Common.SceneLoader
{
    /// <summary>
    /// 씬 전환 시 표시되는 로딩 UI
    /// 씬 전환 후에도 유지되며, 로드 완료 시 숨겨집니다.
    /// </summary>
    [UIAttribute(
        address: "Common/LoadingUI",
        layer: UILayer.Transition,
        useDim: false,
        destroyOnSceneChange: false)]
    public class LoadingUI : UIBase
    {
        /// <summary>
        /// UI가 생성될 때 호출됩니다.
        /// </summary>
        public override void OnSpawn()
        {
            // 초기화 로직
        }

        /// <summary>
        /// UI가 표시될 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public override async UniTask OnShowAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
            // 로딩 애니메이션 시작 등
        }

        /// <summary>
        /// UI가 숨겨질 때 호출됩니다.
        /// </summary>
        /// <param name="ct">CancellationToken</param>
        public override async UniTask OnHideAsync(CancellationToken ct)
        {
            await UniTask.CompletedTask;
            // 로딩 애니메이션 정지 등
        }

        /// <summary>
        /// UI가 제거되기 전에 호출됩니다.
        /// </summary>
        public override void OnBeforeDestroy()
        {
            // 리소스 정리
        }

        /// <summary>
        /// 로딩 진행률을 업데이트합니다.
        /// 커스텀 LoadingUI에서 오버라이드하여 진행률 표시 기능을 구현할 수 있습니다.
        /// </summary>
        /// <param name="progress">진행률 (0.0 ~ 1.0)</param>
        public virtual void UpdateProgress(float progress)
        {
            // 기본 구현은 비어 있음
            // 커스텀 LoadingUI에서 오버라이드하여 Slider, Text 등을 업데이트
        }
    }
}
