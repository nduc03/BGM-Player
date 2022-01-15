using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.Extras;

namespace WpfApp1
{
    struct AppConstants
    {
        public const string USER_ERROR_TITLE = "User Error! Invalid input.";
        public const string DEV_ERROR_TITLE = "Dev-Error! Bug appear.";
        public const string FILE_MISSING = "Audio file missing! Please check again both start and loop file";
        public const string CONFIG_LOCATION = "AudioLoop_Data/path.cfg";
        public const string DATA_LOCATION = "AudioLoop_Data";
    }
    public partial class MainWindow : Window
    {
        private OpenFileDialog loopPath;
        private OpenFileDialog startPath;
        private PathData? pathData;
        private bool isPause = false;

        public MainWindow()
        {
            InitializeComponent();
            startPath = new OpenFileDialog();
            loopPath = new OpenFileDialog();
            pathData = ConfigManager.LoadPath();
            startPath.Filter = "Music files (*.mp3, *.wav)|*.mp3; *.wav";
            loopPath.Filter = "Music files (*.mp3, *.wav)|*.mp3; *.wav";
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;

            if (pathData != null)
            {
                if (File.Exists(pathData.StartPath))
                {
                    StartField.Text = pathData.StartPath;
                    startPath.FileName = pathData.StartPath;
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

        private void play_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(startPath.FileName) || !File.Exists(loopPath.FileName))
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }

            play_button.IsEnabled = false;
            stop_button.IsEnabled = true;
            pause_button.IsEnabled = true;

            if (isPause)
            {
                AudioManager.ContinueAudio();
                isPause = false;
                return;
            }

            AudioManager.InitAudio();
            AudioManager.PlayBGM(startPath.FileName, loopPath.FileName);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            AudioManager.StopAudio();
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
        }

        private void pause_Click(object sender, RoutedEventArgs e)
        {
            isPause = true;
            AudioManager.PauseAudio();
            pause_button.IsEnabled = false;
            play_button.IsEnabled = true;
            stop_button.IsEnabled = true;
        }
    }

    /// <summary>
    /// Manage and control music using NAudio library.
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
        /// Stop current playing music by calling <see cref="StopAudio()"/> and start looping new music.
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
                Console.Error.WriteLine("outputDevice is null or not initialized, please check again", AppConstants.DEV_ERROR_TITLE);
                return;
            }
            StopAudio();
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
