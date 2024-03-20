using System.IO;
using System.Text.Json;
using System.Timers;

namespace bgmPlayer
{
    public static class FileHelper
    {
        private static Timer? timer = null;
        private static object? data = null;

        public static void ApplyState(AppState state)
        {
            if (timer == null)
            {
                timer = new Timer
                {
                    Interval = AppConstants.SAVE_DATA_DELAY,
                    AutoReset = false
                };
                timer.Elapsed += (sender, args) =>
                {
                    SaveData(AppConstants.SAVED_STATE_FILE_NAME);
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                };
                timer.Start();
            }
            data = state;
        }

        public static void InstantSaveState()
        {
            SaveData(AppConstants.SAVED_STATE_FILE_NAME);
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        private static void SaveData(string filename)
        {
            if (data != null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.WriteAllText(Path.Combine(AppConstants.DATA_FOLDER, filename), JsonSerializer.Serialize(data));
                data = null;
            }
        }
    }
}
