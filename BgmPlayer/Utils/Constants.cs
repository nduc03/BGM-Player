namespace bgmPlayer
{
    public readonly ref struct AppConstants
    {
        public const string USER_ERROR_TITLE = "User Error!";
        public const string ERROR_TITLE = "Error!";
        public const string FILE_MISSING = "Audio file missing! Please check again both start and loop file";
        public const string DATA_FOLDER = "BGM_Player_Data";
        public const string SAVED_STATE_FILE_NAME = "state.json";
        public const string SAVED_STATE_LOCATION = $"{DATA_FOLDER}/{SAVED_STATE_FILE_NAME}";
        public const string INTRO_END = "_intro";
        public const string LOOP_END = "_loop";
        public const string CACHE_FOLDER = ".cache";
        public const string THUMBNAIL_CACHE_LOCATION = $"{CACHE_FOLDER}/thumbnail.jpg";
        public const string DEFAULT_MUSIC_TITLE = "BGM Player";
        public const string FILTER = "Wave sound|*.wav|Vorbis|*.ogg";
        public const string VALID_PATH_REGEX = ".(wav|ogg)$";
        public const string AUDIO_DEVICE_ERROR_MSG = """
            Audio devices or drivers error, the audio file(s) cannot be played.
            Choose 'Ok' stop the music.

            In case the problem still occurs, consider close and reopen the app or restart the computer.
            """;
        public const float VOLUME_SCALE = 100f;
        public const int MOUSE_WHEEL_SCALE = 120;
        public const int SAVE_DATA_DELAY = 1000;

        public const string THUMBNAIL_HASH = "133036E793F11F8ABE38F1B9020998C0";
#if ME
        public const string THUMBNAIL_ME_HASH = "1316F87FAEAECDBF2FE42F1B6CE99E92";
        public const string DISABLE_OST_NAME = $"{DATA_FOLDER}/disable_ost_name";
#endif
    }

    public enum AudioPlayerState
    {
        OK, FAILED
    }

    public enum AudioState
    {
        PLAY, PAUSE, STOP
    }

    public enum UpdateMode
    {
        ReplaceAll, Update
    }
}