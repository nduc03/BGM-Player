using System.IO;
using System.Text.Json;

namespace bgmPlayer
{
    public static class FileHelper
    {
        private static System.Timers.Timer? timer = null;
        private static Preferences? data = null;

        // Reduce pressure on hard drive by only save data to RAM first
        // then wait for a delay before actually save the last data on RAM to hard drive
        public static void ApplyPreferences(Preferences preferences)
        {
            if (timer == null)
            {
                timer = new System.Timers.Timer
                {
                    Interval = AppConstants.SAVE_PREFERENCES_DELAY,
                    AutoReset = false
                };
                timer.Elapsed += (sender, args) =>
                {
                    SavePreferences();
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                };
                timer.Start();
            }
            data = preferences;
        }

        public static void InstantSave()
        {
            SavePreferences();
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        private static void SavePreferences()
        {
            if (data != null)
            {
                File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
                data = null;
            }
        }
    }
}
