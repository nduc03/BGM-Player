﻿using System.IO;
using System.Windows;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Extras;
using System.Diagnostics;

namespace bgmPlayer
{
    public partial class MainWindow : Window
    {
        private OpenFileDialog loopPath;
        private OpenFileDialog startPath;
        private PathData? pathData;
        private bool isPause = false;

        public MainWindow()
        {
            if (ConfigManager.MigrateNewConfig() == ReadConfigState.FAILED)
            {
                MessageBox.Show(
                    "Old data file is corrupted. App will reset all configuration.",
                    AppConstants.ERROR_TITLE,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            startPath = new OpenFileDialog();
            loopPath = new OpenFileDialog();

            InitializeComponent();
            InitPathData();
        }

        private void InitPathData()
        {
            if (startPath == null || loopPath == null)
            {
                Trace.TraceWarning("startPath or loopPath is null. Init path again.");
                startPath = new OpenFileDialog();
                loopPath = new OpenFileDialog();
            }
            pathData = ConfigManager.LoadPath();
            startPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            loopPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            if (pathData != null)
            {
                // if pathData not null -> pathData is specified

                // then check pathData is valid path using File.Exists()
                // if path does not exist -> ignore setting up path
                if (File.Exists(pathData.StartPath))
                {
                    StartField.Text = pathData.StartPath; // show path in GUI
                    startPath.FileName = pathData.StartPath; // set path in app logic
                }
                if (File.Exists(pathData.LoopPath))
                {
                    LoopField.Text = pathData.LoopPath;
                    loopPath.FileName = pathData.LoopPath;
                }
            }
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (startPath.ShowDialog() == true)
            {
                StartField.Text = startPath.FileName;
                ConfigManager.SavePath(startPath.FileName, null);
            }
        }

        private void loop_Click(object sender, RoutedEventArgs e)
        {
            if (loopPath.ShowDialog() == true)
            {
                LoopField.Text = loopPath.FileName;
                ConfigManager.SavePath(null, loopPath.FileName);
            }
        }

        private void play_Click(object sender, RoutedEventArgs? e)
        {
            if (!File.Exists(startPath.FileName) && !File.Exists(loopPath.FileName))
            {
                // If both 2 two files is not found or not set -> show error message.
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }

            DisableChooseFile();
            play_button.IsEnabled = false;
            stop_button.IsEnabled = true;
            pause_button.IsEnabled = true;

            if (isPause)
            {
                isPause = false;
                AudioManager.ContinueAudio();
                return;
            }

            if (!File.Exists(startPath.FileName) || !File.Exists(loopPath.FileName))
            {
                // If only one file is not found or not set -> still play music but in loop mode.
                AudioManager.InitAudio();
                AudioManager.PlayLoop(
                        // Check if start path is found -> PlayLoop start path
                        // else -> PlayLoop loop path
                        File.Exists(startPath.FileName) ? startPath.FileName : loopPath.FileName
                );
            }
            else
            {
                AudioManager.InitAudio();
                AudioManager.PlayBGM(startPath.FileName, loopPath.FileName);
            }
        }

        private void stop_Click(object sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.StopAudio();
            EnableChooseFile();
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
        }

        private void pause_Click(object sender, RoutedEventArgs? e)
        {
            isPause = true;
            AudioManager.PauseAudio();
            pause_button.IsEnabled = false;
            play_button.IsEnabled = true;
            stop_button.IsEnabled = true;
        }

        private void DisableChooseFile()
        {
            start.IsEnabled = false;
            loop.IsEnabled = false;
        }

        private void EnableChooseFile()
        {
            start.IsEnabled = true;
            loop.IsEnabled = true;
        }

        /* Play button handler from taskbar */
        private void play_handler(object sender, System.EventArgs e)
        {
            if (play_button.IsEnabled)
                play_Click(sender, null);
        }

        private void pause_handler(object sender, System.EventArgs e)
        {
            if (pause_button.IsEnabled)
                pause_Click(sender, null);
        }

        private void stop_handler(object sender, System.EventArgs e)
        {
            if (stop_button.IsEnabled)
                stop_Click(sender, null);
        }
    }

    /// <summary>
    /// Provide set of funtions to manage and control music using NAudio library.
    /// </summary>
    public static class AudioManager
    {
        private static WaveOutEvent? outputDevice;
        private static AudioFileReader? audioFile;

        public static void InitAudio()
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }
            outputDevice = new WaveOutEvent();
        }

        /// <summary>
        /// Create new audio instance from NAudio then play music.
        /// </summary>
        /// <param name="audioPath">Path to music file</param>
        public static void InitAndPlayAudio(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }
            StopAudio();

            outputDevice = new WaveOutEvent();
            audioFile = new AudioFileReader(audioPath);
            outputDevice.Init(audioFile);
            outputDevice.Play();
        }

        /// <summary>
        /// Start looping new music.
        /// </summary>
        /// <param name="audioPath">Path to music file that needs to loop.</param>
        public static void PlayLoop(string audioPath)
        {
            if (!File.Exists(audioPath))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }
            if (outputDevice == null)
            {
                Trace.TraceWarning("outputDevice is null or not initialized, please check again");
                return;
            }
            AudioFileReader audioFile = new AudioFileReader(audioPath);
            LoopStream loopStream = new LoopStream(audioFile);
            outputDevice.Init(loopStream);
            outputDevice.Play();
        }

        /// <summary>
        /// Not stop current playing music. After music stop, start looping new music.
        /// Require init the audio player first. See <see cref="InitAudio"/>
        /// </summary>
        /// <param name="audioPath">Path to music file that needs to loop.</param>
        public static void AddLoopPending(string audioPath)
        {
            if (outputDevice == null)
            {
                MessageBox.Show("AddLoopPending: outputDevice is null or player is not intialized.", AppConstants.DEV_ERROR_TITLE);
                return;
            }

            LoopStream loopStream = new LoopStream(new AudioFileReader(audioPath));

            outputDevice.PlaybackStopped += (o, e) =>
            {
                outputDevice.Init(loopStream);
                outputDevice.Play();
            };
        }

        /// <summary>
        /// Special method that auto arrange music part and play like infinity BGM.
        /// Infinite BGM require two part: start part and loop path.
        /// Function plays the start music file -> Loop play a second music file.
        /// Require output device to be initialized by calling <see cref="InitAudio"/>
        /// </summary>
        public static void PlayBGM(string startPath, string loopPath)
        {
            if (outputDevice == null) return;
            BGMLoopStream bGMLoopStream = new BGMLoopStream(new AudioFileReader(startPath), new AudioFileReader(loopPath));
            outputDevice.Init(bGMLoopStream);
            outputDevice.Play();
        }

        /// <summary>
        /// Continue the paused audio.
        /// </summary>
        public static void ContinueAudio()
        {
            if (outputDevice == null) return;
            outputDevice.Play();
        }

        /// <summary>
        /// Pause audio.
        /// </summary>
        public static void PauseAudio()
        {
            if (outputDevice == null) return;
            outputDevice.Pause();
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
    }
}
