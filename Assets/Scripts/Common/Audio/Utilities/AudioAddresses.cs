namespace Common.Audio
{
    /// <summary>
    /// 오디오 리소스 주소 상수 정의
    /// 자주 사용되는 오디오 클립 주소를 중앙에서 관리합니다.
    /// </summary>
    public static class AudioAddresses
    {
        // ========== BGM ==========
        public const string BGM_TITLE = "Music/BGM_Title";
        public const string BGM_BATTLE = "Music/BGM_Battle";
        public const string BGM_RESULT = "Music/BGM_Result";

        // ========== SFX - UI ==========
        public const string SFX_BUTTON_CLICK = "SFX/UI/Click";
        public const string SFX_BUTTON_BACK = "SFX/UI/Back";
        public const string SFX_POPUP_OPEN = "SFX/UI/PopupOpen";
        public const string SFX_POPUP_CLOSE = "SFX/UI/PopupClose";

        // ========== SFX - Game ==========
        public const string SFX_EXPLOSION = "SFX/Game/Explosion";
        public const string SFX_FOOTSTEP = "SFX/Game/Footstep";

        // ========== Voice ==========
        public const string VOICE_INTRO = "Voice/Intro";
        public const string VOICE_TUTORIAL = "Voice/Tutorial";
    }
}
