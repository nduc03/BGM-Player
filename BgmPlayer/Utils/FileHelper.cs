﻿using System.IO;
using System.Text.Json;
using System.Windows;

namespace bgmPlayer
{
    public static class FileHelper
    {
        private static System.Timers.Timer? timer = null;
        private static PersistedState? data = null;

        // Reduce pressure on hard drive by only save data to RAM first
        // then wait for a delay before saving the last data on RAM to hard drive
        public static void ApplyState(PersistedState state)
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
                    SaveData(AppConstants.SAVED_STATE_LOCATION);
                    timer.Stop();
                    timer.Dispose();
                    timer = null;
                };
                timer.Start();
            }
            data = state;
        }

        public static void InstantSave()
        {
            SaveData(AppConstants.SAVED_STATE_LOCATION);
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }
        }

        private static void SaveData(string path)
        {
            if (data != null)
            {
                try
                {
                    File.WriteAllText(path, JsonSerializer.Serialize(data));
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Data directory not found! Data cannot be saved.", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                data = null;
            }
        }
    }
}
