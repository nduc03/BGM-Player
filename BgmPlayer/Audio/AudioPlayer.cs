﻿using NAudio.Extras;
using NAudio.Wave;
using System;
using System.Diagnostics;
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

        private static void Initialize()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            outputDevice = new()
            {
                DesiredLatency = 400,
                NumberOfBuffers = 2
            };
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
                Initialize();
                if (audioPath.EndsWith(".ogg"))
                {
                    VorbisLoopStream loopStream = new(audioPath);
                    outputDevice!.Init(loopStream);
                }
                else
                {
                    LoopStream loopStream = new(new AudioFileReader(audioPath));
                    outputDevice!.Init(loopStream);
                }
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
                Initialize();
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
            if (outputDevice == null)
            {
                Debug.WriteLine("SetVolume: outputDevice = null");
                return AudioPlayerState.FAILED;
            }
            try
            {
                outputDevice.Volume = Math.Clamp(Volume, 0.0f, 1.0f);
            }
            catch
            {
                return AudioPlayerState.FAILED;
            }
            volume = Math.Clamp(Volume, 0.0f, 1.0f);

            return AudioPlayerState.OK;
        }
    }
}
