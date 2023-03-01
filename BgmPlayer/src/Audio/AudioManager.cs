using System.IO;
using System.Diagnostics;
using System;
using System.Windows;
using NAudio.Wave;
using NAudio.Vorbis;

namespace bgmPlayer
{
    /// <summary>
    /// Provide set of funtions to manage and control music using NAudio library.
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
                return AudioManagerState.FAILED;
            }
            try
            {
                InitAudio();
                if (audioPath.EndsWith(".ogg"))
                {
                    VorbisBGMLoopStream loopStream = new(audioPath);
                    outputDevice!.Init(loopStream);
                }
                else
                {
                    BGMLoopStream loopStream = new(new AudioFileReader(audioPath));
                    outputDevice!.Init(loopStream);
                }
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
        /// Special method that auto arrange music part and play an infinite BGM.
        /// An infinite BGM requires two part: start part and loop part.
        /// Function plays from start music file then plays a loop music file unlimited times.
        /// An output device need to be initialized by calling <see cref="InitAudio"/>
        /// </summary>
        public static AudioManagerState PlayBGM(string introPath, string loopPath)
        {
            try
            {
                InitAudio();
                if (introPath.EndsWith(".ogg") && loopPath.EndsWith(".ogg"))
                {
                    VorbisBGMLoopStream bgmLoopStream = new(introPath, loopPath);
                    outputDevice!.Init(bgmLoopStream);
                }
                else
                {
                    BGMLoopStream bgmLoopStream = new(new AudioFileReader(introPath), new AudioFileReader(loopPath));
                    outputDevice!.Init(bgmLoopStream);

                }
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
            //     <= 0 => 0, // wtf is this :))
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
            else return Math.Clamp(outputDevice.Volume, 0, 100);
        }
    }
}
