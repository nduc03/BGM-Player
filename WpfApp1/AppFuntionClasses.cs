using System.IO;
using System.Text.Json;
using NAudio.Wave;
using NAudio.Extras;
using System.Diagnostics;

namespace bgmPlayer
{
    struct AppConstants
    {
        public const string USER_ERROR_TITLE = "User Error! Invalid input";
        public const string DEV_ERROR_TITLE = "Dev-Error! Bug appear";
        public const string ERROR_TITLE = "Error!";
        public const string FILE_MISSING = "Audio file missing! Please check again both start and loop file";
        public const string DATA_FOLDER = "BGM_Player_Data";
        public const string OLD_DATA_FOLDER = "AudioLoop_Data";
        public const string OLD_CONFIG_LOCATION = $"{OLD_DATA_FOLDER}/path.cfg";
        public const string CONFIG_LOCATION = $"{DATA_FOLDER}/data.json";
        public const float VOLUME_SCALE = 10f;
    }

    public enum ReadConfigState
    {
        SUCCESSFUL, FAILED, FILE_NOT_FOUND
    }

    public class BGMLoopStream : LoopStream
    {
        private WaveStream? startStream;

        public BGMLoopStream(WaveStream startStream, WaveStream loopStream) : base(loopStream)
        {
            this.startStream = startStream;
        }

        public BGMLoopStream(WaveStream loopStream) : base(loopStream) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (startStream == null)
                return base.Read(buffer, offset, count);

            int startRead = startStream.Read(buffer, offset, count);
            int loopRead = 0;
            if (startRead < count)
            {
                // startRead < count when the startStream near the end of stream.
                // when near the end, block the code until startStream finish read
                // then call loopStream read forever
                while (startStream.Position < startStream.Length)
                {
                    // do nothing
                    // just block loopStream read until startStream is fully read
                    // and avoid overlap between the end of startStream and the start of loopStream
                }
                // loopStream started loop here and loop until stop or close the app.
                loopRead = base.Read(buffer, 0, count);
            }
            return startRead + loopRead;
        }
    }
    public class ConfigData
    {
        public string? StartPath { get; set; }
        public string? LoopPath { get; set; }
        public float? Volume { get; set; }
    }

    public static class ConfigManager
    {
        public static ConfigData? LoadPath()
        {
            ConfigData? data;
            if (!File.Exists(AppConstants.CONFIG_LOCATION)) return null;

            string fileContent = File.ReadAllText(AppConstants.CONFIG_LOCATION);
            try
            {
                data = JsonSerializer.Deserialize<ConfigData>(fileContent);
            }
            catch
            {
                Trace.TraceWarning("Json error");
                data = null;
            }
            return data;
        }

        public static void SavePath(string? startPath, string? loopPath)
        {
            ConfigData? data = LoadPath();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.Create(AppConstants.CONFIG_LOCATION).Close();
                data = new ConfigData();
            }

            if (startPath != null) data.StartPath = startPath;
            if (loopPath != null) data.LoopPath = loopPath;
            File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
        }

        public static void SaveVolume(float? Volume)
        {
            ConfigData? data = LoadPath();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.Create(AppConstants.CONFIG_LOCATION).Close();
                data = new ConfigData();
            }

            if (Volume != null) data.Volume = Volume;
            File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
        }

        public static ReadConfigState MigrateNewConfig()
        {
            if (!File.Exists(AppConstants.OLD_CONFIG_LOCATION))
            {
                if (Directory.Exists(AppConstants.OLD_DATA_FOLDER))
                {
                    Directory.Delete(AppConstants.OLD_DATA_FOLDER, true);
                }
                return ReadConfigState.FILE_NOT_FOUND;
            }

            ConfigData? data;
            try
            {
                data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(AppConstants.OLD_CONFIG_LOCATION));
            }
            catch
            {
                data = null;
            }

            if (data == null)
            {
                Trace.TraceWarning("Old config corrupted, app will ignore migrate and only delete old config file.");
                Directory.Delete(AppConstants.OLD_DATA_FOLDER, true);
                File.Delete(AppConstants.OLD_CONFIG_LOCATION);
                return ReadConfigState.FAILED;
            }

            SavePath(data.StartPath, data.LoopPath);
            File.Delete(AppConstants.OLD_CONFIG_LOCATION);
            Directory.Delete(AppConstants.OLD_DATA_FOLDER, true);
            return ReadConfigState.SUCCESSFUL;
        }
    }
}
