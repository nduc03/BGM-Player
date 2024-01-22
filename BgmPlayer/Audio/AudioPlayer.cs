using NAudio.Extras;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using Windows.Media;

namespace bgmPlayer
{
    /// <summary>
    /// Organized helper class for managing NAudio fearture for bgmPlayer
    /// Provide set of functions to manage and control music by using NAudio library.
    /// </summary>
    public static class AudioPlayer
    {
        private static WaveOutEvent? outputDevice;
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
        public static bool IsPlaying { get => CurrentState == AudioState.PLAY; }
        public static bool IsPaused { get => CurrentState == AudioState.PAUSE; }

        [MemberNotNull(nameof(outputDevice))]
        private static void Initialize(IWaveProvider stream)
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            outputDevice = new()
            {
                DesiredLatency = 400,
                NumberOfBuffers = 2,
            };
            outputDevice.Init(stream);
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
                return AudioPlayerState.FAILED;
            }
            try
            {
                IWaveProvider stream;
                if (audioPath.EndsWith(".ogg"))
                {
                    stream = new VorbisLoopStream(audioPath);
                }
                else
                {
                    stream = new LoopStream(new AudioFileReader(audioPath));
                }
                Initialize(stream);
                outputDevice.Play();
                SMTCManager.Enable();
                SMTCManager.UpdateStatus(MediaPlaybackStatus.Playing);
                CurrentState = AudioState.PLAY;
                StateChanged?.Invoke(AudioState.PLAY);
                SMTCManager.UpdateTitle(audioPath);
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
        /// If both <paramref name="introPath"/> and <paramref name="loopPath"/> are specified, play looped BGM with 
        /// intro segment from <paramref name="introPath"/>. Note that two segments must have the same audio format.
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
                IWaveProvider bgmStream;
                if (introPath.EndsWith(".ogg") && loopPath.EndsWith(".ogg"))
                {
                    bgmStream = new VorbisBGMLoopStream(introPath, loopPath);
                }
                else
                {
                    bgmStream = new BGMLoopStream(new AudioFileReader(introPath), new AudioFileReader(loopPath));
                }
                Initialize(bgmStream);
                outputDevice.Play();
                SMTCManager.Enable();
                SMTCManager.UpdateStatus(MediaPlaybackStatus.Playing);
                CurrentState = AudioState.PLAY;
                StateChanged?.Invoke(AudioState.PLAY);
                SMTCManager.UpdateTitle(introPath, loopPath);
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
            try
            {
                outputDevice.Play();
            }
            catch
            {
                return AudioPlayerState.FAILED;
            }
            SMTCManager.UpdateStatus(MediaPlaybackStatus.Playing);
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
            SMTCManager.UpdateStatus(MediaPlaybackStatus.Paused);
            CurrentState = AudioState.PAUSE;
            StateChanged?.Invoke(AudioState.PAUSE);
            return AudioPlayerState.OK;
        }

        /// <summary>
        /// Stop playing audio in app and dispose the outputDevice.
        /// </summary>
        public static void Stop()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }
            SMTCManager.UpdateStatus(MediaPlaybackStatus.Stopped);
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

            if (!ChangeVolume()) return AudioPlayerState.FAILED;

            return AudioPlayerState.OK;
        }

        private static bool ChangeVolume()
        {
            if (outputDevice == null)
            {
                return false;
            }
            try
            {
                outputDevice.Volume = volume;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
