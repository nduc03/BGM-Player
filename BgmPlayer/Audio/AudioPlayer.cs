using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Windows.Media;

namespace bgmPlayer
{
    /// <summary>
    /// Wrapper of NAudio library for bgmPlayer
    /// Provide set of functions to manage and control music by using NAudio library.
    /// </summary>
    public static class AudioPlayer
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? audioFile;
        private static float volume = 1f;

        public static AudioState CurrentState = AudioState.STOP;

        public delegate void PlayState(AudioState state);
        public static event PlayState? StateChanged;
        public static bool IsStopped
        {
            get
            {
                if (outputDevice == null) return true;

                if (outputDevice.PlaybackState == PlaybackState.Stopped) return true;
                else return false;
            }
        }
        public static bool IsPlaying { get { return CurrentState == AudioState.PLAY; } }
        public static bool IsPause { get { return CurrentState == AudioState.PAUSE; } }

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
        /// Play looped music seamlessly.
        /// </summary>
        /// <param name="audioPath">Path to music file that needs to loop.</param>
        public static AudioPlayerState PlayLoop(string? audioPath)
        {
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return AudioPlayerState.FAILED;
            }
            try
            {
                InitAudio();
                if (audioPath.EndsWith(".ogg"))
                {
                    VorbisLoopStream loopStream = new(audioPath);
                    outputDevice!.Init(loopStream);
                }
                else
                {
                    BGMLoopStream loopStream = new(new AudioFileReader(audioPath));
                    outputDevice!.Init(loopStream);
                }
                outputDevice.Play();
                SMTCHelper.IsEnable = true;
                SMTCHelper.UpdateStatus(MediaPlaybackStatus.Playing);
                CurrentState = AudioState.PLAY;
                StateChanged?.Invoke(AudioState.PLAY);
                SMTCHelper.UpdateTitle(null, audioPath);
                return AudioPlayerState.OK;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return AudioPlayerState.FAILED;
            }
        }

        /// <summary>
        /// Play seamless BGM with optional intro segment.
        /// If both <paramref name="introPath"/> and <paramref name="loopPath"/> is specified, play looped BGM with 
        /// intro segment from <paramref name="introPath"/>.
        /// If intro segment is not needed, <paramref name="introPath"/> and <paramref name="loopPath"/> have the same
        /// purpose, set one path and leave the remaining param to <c>string.Empty</c> or <c>null</c>.
        /// </summary>
        public static AudioPlayerState PlayBGM(string? introPath, string? loopPath)
        {
            if (introPath == null || loopPath == null)
            {
                return PlayLoop(introPath ?? loopPath);
            }
            if (introPath == string.Empty || loopPath == string.Empty)
            {
                return PlayLoop(introPath != string.Empty ? introPath : loopPath);
            }
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
                SMTCHelper.IsEnable = true;
                SMTCHelper.UpdateStatus(MediaPlaybackStatus.Playing);
                CurrentState = AudioState.PLAY;
                StateChanged?.Invoke(AudioState.PLAY);
                SMTCHelper.UpdateTitle(introPath, loopPath);
                return AudioPlayerState.OK;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return AudioPlayerState.FAILED;
            }
        }

        /// <summary>
        /// Continue the paused audio.
        /// </summary>
        public static AudioPlayerState Continue()
        {
            if (outputDevice == null) return AudioPlayerState.FAILED;
            SetVolume(volume);
            outputDevice.Play();
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Playing);
            CurrentState = AudioState.PLAY;
            StateChanged?.Invoke(AudioState.PLAY);
            return AudioPlayerState.OK;
        }

        /// <summary>
        /// Pause audio.
        /// </summary>
        public static AudioPlayerState Pause()
        {
            if (outputDevice == null) return AudioPlayerState.FAILED;
            outputDevice.Pause();
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Paused);
            CurrentState = AudioState.PAUSE;
            StateChanged?.Invoke(AudioState.PAUSE);
            return AudioPlayerState.OK;
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
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Stopped);
            CurrentState = AudioState.STOP;
            StateChanged?.Invoke(AudioState.STOP);
        }

        /// <summary>
        /// Set volume for audio.
        /// <paramref name="Volume"/>: Should be between 0f and 1f
        /// </summary>
        public static AudioPlayerState SetVolume(float Volume)
        {
            volume = Math.Clamp(Volume, 0.0f, 1.0f);
            if (outputDevice == null)
            {
                Debug.WriteLine("SetVolume: outputDevice = null");
                return AudioPlayerState.FAILED;
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

            return AudioPlayerState.OK;
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
