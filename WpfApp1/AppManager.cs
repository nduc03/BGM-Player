using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Windows;
using NAudio.Wave;

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

    public enum AudioManagerState
    {
        OK, FILE_MISSING, PLAY_FAILED
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

    /// <summary>
    /// Provide set of funtions to manage and control music using NAudio library.
    /// </summary>
    public static class AudioManager
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? audioFile;
        private static float volume = 1f;
        public static bool IsStopped
        {
            get
            {
                if (outputDevice == null) return true;

                if (outputDevice.PlaybackState == PlaybackState.Stopped) return true;
                else return false;
            }
        }
        public static bool IsPlaying { get { return !IsStopped; } }

        public static void InitAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            outputDevice = new WaveOutEvent();
            SetVolume(volume);
        }

        /// <summary>
        /// Create new audio instance from NAudio then play music.
        /// </summary>
        /// <param name="audioPath">Path to music file</param>
        public static AudioManagerState InitAndPlayAudio(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return AudioManagerState.FILE_MISSING;
            }
            StopAudio();

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(audioPath);
            outputDevice.Init(audioFile);
            SetVolume(volume);
            outputDevice.Play();
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Start looping new music.
        /// </summary>
        /// <param name="audioPath">Path to music file that needs to loop.</param>
        public static AudioManagerState PlayLoop(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return AudioManagerState.FILE_MISSING;
            }
            if (outputDevice == null)
            {
                Trace.TraceWarning("outputDevice is null or not initialized, please check again");
                return AudioManagerState.PLAY_FAILED;
            }
            AudioFileReader audioFile = new AudioFileReader(audioPath);
            BGMLoopStream loopStream = new BGMLoopStream(audioFile);
            outputDevice.Init(loopStream);
            outputDevice.Play();
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Special method that auto arrange music part and play infinity BGM.
        /// Infinite BGM require two part: start part and loop path.
        /// Function plays the start music file -> Loop play a second music file.
        /// Require output device to be initialized by calling <see cref="InitAudio"/>
        /// </summary>
        public static AudioManagerState PlayBGM(string startPath, string loopPath)
        {
            if (outputDevice == null) return AudioManagerState.PLAY_FAILED;
            BGMLoopStream bGMLoopStream = new BGMLoopStream(new AudioFileReader(startPath), new AudioFileReader(loopPath));
            outputDevice.Init(bGMLoopStream);
            SetVolume(volume);
            outputDevice.Play();
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Continue the paused audio.
        /// </summary>
        public static AudioManagerState ContinueAudio()
        {
            if (outputDevice == null) return AudioManagerState.PLAY_FAILED;
            SetVolume(volume);
            outputDevice.Play();
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Pause audio.
        /// </summary>
        public static AudioManagerState PauseAudio()
        {
            if (outputDevice == null) return AudioManagerState.PLAY_FAILED;
            outputDevice.Pause();
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Stop playing audio in app. If no playing audio, no action will be performed.
        /// </summary>
        public static void StopAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }
        }

        /// <summary>
        /// Set volume for audio.
        /// <paramref name="Volume"/>: Should be between 0f and 1f
        /// </summary>
        public static AudioManagerState SetVolume(float Volume)
        {
            volume = Volume;
            if (outputDevice == null)
            {
                Trace.TraceWarning("SetVolume: outputDevice = null");
                return AudioManagerState.PLAY_FAILED;
            }
            switch (Volume)
            {
                case (<= 0): outputDevice.Volume = 0; break;
                case (> 1): outputDevice.Volume = 1; break;
                default: outputDevice.Volume = Volume; break;
            }
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Get the current volume value
        /// </summary>
        /// <returns>The volume value between 0f and 1f</returns>
        public static float GetVolume()
        {
            if (outputDevice == null)
            {
                Trace.TraceWarning("SetVolume: outputDevice = null");
                return 0;
            }
            else return outputDevice.Volume;
        }
    }
}
