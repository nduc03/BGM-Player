namespace bgmPlayer
{
    struct AppConstants
    {
        public const string USER_ERROR_TITLE = "User Error! Invalid input";
        public const string DEV_ERROR_TITLE = "Dev-Error! Bug appear";
        public const string ERROR_TITLE = "Error!";
        public const string FILE_MISSING = "Audio file missing! Please check again both start and loop file";
        public const string DATA_FOLDER = "BGM_Player_Data";
        public const string CONFIG_LOCATION = $"{DATA_FOLDER}/preferences.json";
        public const string INTRO_END = "_intro";
        public const string LOOP_END = "_loop";
        public const string CACHE_FOLDER = ".cache";
        public const string THUMBNAIL_CACHE_LOCATION = $"{CACHE_FOLDER}/thumbnail.jpg";
        public const string DEFAULT_MUSIC_TITLE = "BGM Player";

        public const float VOLUME_SCALE = 100f;
        public const int MOUSE_WHEEL_SCALE = 120;
        public const int SAVE_PREFERENCES_DELAY = 1000;

        public const string THUMBNAIL_HASH = "133036E793F11F8ABE38F1B9020998C0";
#if ME
        public const string THUMBNAIL_ME_HASH = "1316F87FAEAECDBF2FE42F1B6CE99E92";
        public const string DISABLE_OST_NAME = $"{DATA_FOLDER}/disable_ost_name";
#endif
    }

    public enum AudioManagerState
    {
        OK, FILE_MISSING, FAILED
    }

    public enum AudioState
    {
        PLAY, PAUSE, STOP
    }
}