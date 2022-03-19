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
        OK, FILE_MISSING, FAILED
    }

    public enum AudioState
    {
        PLAY, PAUSE, STOP
    }

    public class ConfigData
    {
        public string? IntroPath { get; set; }
        public string? LoopPath { get; set; }
        public int? Volume { get; set; }
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

        public static void SaveConfig(string? IntroPath = null, string? LoopPath = null, int? Volume = null, bool? AutoFill = null)
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
    /// Provide set of funtions to manage and control music through NAudio library.
    /// </summary>
    public static class AudioManager
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? audioFile;
        private static float volume = 1f;

        public static AudioState AudioState = AudioState.STOP;
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

        private static void InitAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            outputDevice = new();
            SetVolume(volume);
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
            try
            {
                InitAudio();
                AudioFileReader audioFile = new(audioPath);
                BGMLoopStream loopStream = new(audioFile);
                outputDevice!.Init(loopStream);
                outputDevice.Play();
                AudioState = AudioState.PLAY;
                return AudioManagerState.OK;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return AudioManagerState.FAILED;
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
            try
            {
                InitAudio();
                BGMLoopStream bgmLoopStream = new(new AudioFileReader(introPath), new AudioFileReader(loopPath));
                outputDevice!.Init(bgmLoopStream);
                SetVolume(volume);
                outputDevice.Play();
                AudioState = AudioState.PLAY;
                return AudioManagerState.OK;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return AudioManagerState.FAILED;
            }
        }

        /// <summary>
        /// Continue the paused audio.
        /// </summary>
        public static AudioManagerState Continue()
        {
            if (outputDevice == null) return AudioManagerState.FAILED;
            SetVolume(volume);
            outputDevice.Play();
            AudioState = AudioState.PLAY;
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Pause audio.
        /// </summary>
        public static AudioManagerState Pause()
        {
            if (outputDevice == null) return AudioManagerState.FAILED;
            outputDevice.Pause();
            AudioState = AudioState.PAUSE;
            return AudioManagerState.OK;
        }

        /// <summary>
        /// Stop playing audio in app. If no playing audio, no action will be performed.
        /// </summary>
        public static void Stop()
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
            AudioState = AudioState.STOP;
        }

        /// <summary>
        /// Set volume for audio.
        /// <paramref name="Volume"/>: Should be between 0f and 1f
        /// </summary>
        public static AudioManagerState SetVolume(float Volume)
        {
            volume = Math.Clamp(Volume, 0.0f, 1.0f);
            if (outputDevice == null)
            {
                Debug.WriteLine("SetVolume: outputDevice = null");
                return AudioManagerState.FAILED;
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
                Debug.WriteLine("GetVolume: outputDevice = null");
                return 0;
            }
            else return outputDevice.Volume;
        }
    }
}
