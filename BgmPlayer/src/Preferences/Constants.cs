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
        public const string OLD_DATA_FOLDER = "BGM_Player_Data";
        public const string OLD_CONFIG_LOCATION = $"{OLD_DATA_FOLDER}/data.json";
        public const string INTRO_END = "_intro";
        public const string LOOP_END = "_loop";
        public const string CACHE_FOLDER = ".cache";
        public const float VOLUME_SCALE = 100f;
        public const int MOUSE_WHEEL_SCALE = 120;
        public const int SAVE_PREFERENCES_DELAY = 1000;
    }

    public enum ReadConfigState
    {
        SUCCESSFUL, FAILED, FILE_NOT_FOUND
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