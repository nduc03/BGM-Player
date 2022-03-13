using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Windows;
using NAudio.Wave;
using System;

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
        public string? IntroPath { get; set; }
        public string? LoopPath { get; set; }
        public float? Volume { get; set; }
        public bool? AutoFill { get; set; }
    }

    public static class ConfigManager
    {
        public static ConfigData? LoadConfig()
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
                Debug.WriteLine("Json error");
                data = null;
            }
            return data;
        }

        public static void SaveConfig(string? IntroPath = null, string? LoopPath = null, float? Volume = null, bool? AutoFill = null)
        {
            ConfigData? data = LoadConfig();
            if (data == null)
            {
                Directory.CreateDirectory(AppConstants.DATA_FOLDER);
                File.Create(AppConstants.CONFIG_LOCATION).Close();
                data = new ConfigData();
            }

            if (IntroPath != null) data.IntroPath = IntroPath;
            if (LoopPath != null) data.LoopPath = LoopPath;
            if (Volume != null) data.Volume = Volume;
            if (AutoFill != null) data.AutoFill = AutoFill;

            File.WriteAllText(AppConstants.CONFIG_LOCATION, JsonSerializer.Serialize(data));
        }

        public static ReadConfigState MigrateNewConfig()
        {
            if (!File.Exists(AppConstants.OLD_CONFIG_LOCATION))
            {
                if (Directory.Exists(AppConstants.OLD_DATA_FOLDER))
                {
                    SafeDeleteOldConfigFolder();
                }
                return ReadConfigState.FILE_NOT_FOUND;
            }

            ConfigData? data;
            try
            {
                data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(AppConstants.OLD_CONFIG_LOCATION));
            }
            catch (JsonException)
            {
                data = null;
            }

            if (data == null)
            {
                Debug.WriteLine("Old config corrupted, app will ignore migrate and only delete old config file.");
                Directory.Delete(AppConstants.OLD_DATA_FOLDER, true);
                File.Delete(AppConstants.OLD_CONFIG_LOCATION);
                return ReadConfigState.FAILED;
            }

            SaveConfig(data.IntroPath, data.LoopPath, data.Volume);
            File.Delete(AppConstants.OLD_CONFIG_LOCATION);
            SafeDeleteOldConfigFolder();
            return ReadConfigState.SUCCESSFUL;
        }

        private static void SafeDeleteOldConfigFolder()
        {
            try
            {
                Directory.Delete(AppConstants.OLD_DATA_FOLDER, false);
            }
            catch (IOException)
            {
                Debug.WriteLine("MigrateNewConfig did not delete old folder, " +
                    "it is normal if old config has the same folder with the new one, otherwise it might be a bug.");
            }
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
                Debug.WriteLine("outputDevice is null or not initialized, please check again");
                return AudioManagerState.PLAY_FAILED;
            }
            try
            {
                AudioFileReader audioFile = new(audioPath);
                BGMLoopStream loopStream = new(audioFile);
                outputDevice.Init(loopStream);
                outputDevice.Play();
                return AudioManagerState.OK;
            }
            catch
            {
                return AudioManagerState.PLAY_FAILED;
            }
        }

        /// <summary>
        /// Special method that auto arrange music part and play infinity BGM.
        /// Infinite BGM require two part: start part and loop path.
        /// Function plays the start music file -> Loop play a second music file.
        /// Require output device to be initialized by calling <see cref="InitAudio"/>
        /// </summary>
        public static AudioManagerState PlayBGM(string introPath, string loopPath)
        {
            if (outputDevice == null) return AudioManagerState.PLAY_FAILED;
            try
            {
                BGMLoopStream bgmLoopStream = new(new AudioFileReader(introPath), new AudioFileReader(loopPath));
                outputDevice.Init(bgmLoopStream);
                SetVolume(volume);
                outputDevice.Play();
                return AudioManagerState.OK;
            }
            catch
            {
                return AudioManagerState.PLAY_FAILED;
            }
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
                Debug.WriteLine("SetVolume: outputDevice = null");
                return AudioManagerState.PLAY_FAILED;
            }

            // Decided not delete this old code because it is fun
            // Old normal code:
            // switch (Volume)
            // {
            //     case <= 0: outputDevice.Volume = 0; break;
            //     case > 1: outputDevice.Volume = 1; break;
            //     default: outputDevice.Volume = Volume; break;
            // }
            // Visual Studio suggest this new and fun code:
            // outputDevice.Volume = Volume switch
            // {
            //     <= 0 => 0, // wtf is this :) 
            //     > 1 => 1,
            //     _ => Volume,
            // };
            // Better new code:
            outputDevice.Volume = Math.Clamp(Volume, 0.0f, 1.0f);

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
                Debug.WriteLine("SetVolume: outputDevice = null");
                return 0;
            }
            else return outputDevice.Volume;
        }
    }
}
