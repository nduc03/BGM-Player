using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace bgmPlayer
{
    public static class PreferencesHelper
    {
        public static Preferences? LoadPreferences()
        {
            Preferences? data;
            if (!File.Exists(AppConstants.CONFIG_LOCATION)) return null;

            string fileContent = File.ReadAllText(AppConstants.CONFIG_LOCATION);
            try
            {
                data = JsonSerializer.Deserialize<Preferences>(fileContent);
            }
            catch
            {
                Debug.WriteLine("Json error");
                data = null;
            }
            return data;
        }

        public static void SavePreferences(
            string? IntroPath = null,
            string? LoopPath = null,
            float? Volume = null,
            bool? AutoFill = null
        )
        {
            Preferences? data = LoadPreferences();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.Create(AppConstants.CONFIG_LOCATION).Close();
                data = new Preferences();
            }

            if (IntroPath != null) data.IntroPath = IntroPath;
            if (LoopPath != null) data.LoopPath = LoopPath;
            if (Volume != null) data.Volume = Volume;
            if (AutoFill != null) data.AutoFill = AutoFill;

            File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
        }
    }
}
