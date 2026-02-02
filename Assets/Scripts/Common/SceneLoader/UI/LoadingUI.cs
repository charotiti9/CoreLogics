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
    }
}
