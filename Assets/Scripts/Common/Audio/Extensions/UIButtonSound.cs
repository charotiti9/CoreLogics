using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Common.Audio
{
    /// <summary>
    /// UI 버튼 클릭 사운드 자동 재생
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour
    {
        [SerializeField] private string clickSoundAddress = AudioAddresses.SFX_BUTTON_CLICK;
        [SerializeField] private float volume = 1f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            AudioManager.Instance.PlaySFXAsync(clickSoundAddress, volume, ct: destroyCancellationToken).Forget();
        }
    }
}
