using System.IO;
using System.Text.Json;
using System.Timers;

namespace bgmPlayer
{
    public static class FileHelper
    {
        private static readonly object _lock = new();
        private static Timer? timer = null;
        private static object? data = null;

        public static void ApplyState(AppState state)
        {
            lock (_lock)
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
        }

        public static void InstantSaveState()
        {
            lock (_lock)
            {
                SaveData(AppConstants.SAVED_STATE_FILE_NAME);
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                }
            }
        }

        private static void SaveData(string filename)
        {
            if (data == null) return;

            Directory.CreateDirectory(AppConstants.DATA_FOLDER);

            var json = JsonSerializer.Serialize(data);

            WriteFileAtomic(Path.Combine(AppConstants.DATA_FOLDER, filename), json);

            data = null;
        }

        private static void WriteFileAtomic(string path, string content)
        {
            var tempFile = path + ".tmp";

            File.WriteAllText(tempFile, content);

            File.Move(tempFile, path, true);
        }
    }
}
