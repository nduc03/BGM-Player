using NAudio;
using NAudio.Extras;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
        private static AudioState currentState = AudioState.STOP;
        private static FadeInOutSampleProvider? fadeStream;
        private static IWaveProvider? stream;
        private static System.Timers.Timer? fadeTimer;
        private static long pausedPosition = 0;

        public static AudioState CurrentState
        {
            get => currentState;
            private set { currentState = value; }
        }

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

        [MemberNotNull(new string[] { nameof(outputDevice), nameof(fadeStream) })]
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
            fadeStream = new FadeInOutSampleProvider(new WaveToSampleProvider(stream));
            outputDevice.Init(fadeStream);
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
                SMTCManager.PlaybackStatus = MediaPlaybackStatus.Playing;
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
                if (introPath.EndsWith(".ogg") && loopPath.EndsWith(".ogg"))
                {
                    stream = new VorbisBGMLoopStream(introPath, loopPath);
                }
                else
                {
                    stream = new BGMLoopStream(new AudioFileReader(introPath), new AudioFileReader(loopPath));
                }
                Initialize(stream);
                outputDevice.Play();
                SMTCManager.Enable();
                SMTCManager.PlaybackStatus = MediaPlaybackStatus.Playing;
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
            catch (MmException)
            {
                if (stream == null) return AudioPlayerState.FAILED;
                Initialize(stream);
                if (stream is Stream s) s.Position = pausedPosition;
                else return AudioPlayerState.FAILED;
                outputDevice.Play();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                return AudioPlayerState.FAILED;
            }
            SMTCManager.PlaybackStatus = MediaPlaybackStatus.Playing;
            CurrentState = AudioState.PLAY;
            StateChanged?.Invoke(AudioState.PLAY);
            return AudioPlayerState.OK;
        }

        /// <summary>
        /// Pause audio.
        /// </summary>
        public static AudioPlayerState Pause()
        {
            if (outputDevice == null || stream == null) return AudioPlayerState.FAILED;
            outputDevice.Pause();
            pausedPosition = (stream as Stream)?.Position ?? 0;
            SMTCManager.PlaybackStatus = MediaPlaybackStatus.Paused;
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
            SMTCManager.PlaybackStatus = MediaPlaybackStatus.Stopped;
            if (currentState != AudioState.STOP) StateChanged?.Invoke(AudioState.STOP);
            CurrentState = AudioState.STOP;
        }

        /// <summary>
        /// Fade audio before stopped.
        /// </summary>
        public static void StopFade(int second)
        {
            if (fadeTimer != null) return;

            fadeStream?.BeginFadeOut(second * 1000);

            fadeTimer = new()
            {
                Interval = second * 1000,
                AutoReset = false
            };

            fadeTimer.Elapsed += (sender, args) =>
            {
                fadeTimer.Dispose();
                fadeTimer = null;
                Stop();
            };
            fadeTimer.Start();
        }

        /// <summary>
        /// Set volume for audio.
        /// <paramref name="Volume"/>: Should be between 0f and 1f
        /// </summary>
        public static AudioPlayerState SetVolume(float Volume)
        {
            // even if failed to get the outputDevice the volume state still update
            // so when init device again, it will set to correct volume
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
