using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace bgmPlayer
{
    public static class PersistedStateManager
    {
        private static PersistedState? dataCache = null;

        public static PersistedState? LoadState()
        {
            if (dataCache != null) return dataCache;

            PersistedState? data;
            if (!File.Exists(AppConstants.SAVED_STATE_LOCATION)) return null;

            string fileContent = File.ReadAllText(AppConstants.SAVED_STATE_LOCATION);
            try
            {
                data = JsonSerializer.Deserialize<PersistedState>(fileContent);
            }
            catch
            {
                Debug.WriteLine("Json error");
                data = null;
            }
            dataCache = data;
            return data;
        }

        public static void SaveState(
            string? IntroPath = null,
            string? LoopPath = null,
            float? Volume = null,
            bool? AutoFill = null
        )
        {
            PersistedState? data = dataCache ?? LoadState();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.Create(AppConstants.SAVED_STATE_LOCATION).Close();
                data = new PersistedState();
            }

            if (IntroPath != null) data.IntroPath = IntroPath;
            if (LoopPath != null) data.LoopPath = LoopPath;
            if (Volume != null) data.Volume = Volume;
            if (AutoFill != null) data.AutoFill = AutoFill;

            dataCache = data;
            FileHelper.ApplyState(data);
        }
    }
}
