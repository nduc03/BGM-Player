using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using Windows.Media;
using Windows.Storage.Streams;

namespace bgmPlayer
{
    public partial class MainWindow : Window
    {
        private readonly ConfigData? configData;
        private readonly Windows.Media.Playback.MediaPlayer mediaPlayer;
        private readonly SystemMediaTransportControls smtc;
        private readonly SystemMediaTransportControlsDisplayUpdater updater;
        private OpenFileDialog loopPath;
        private OpenFileDialog startPath;
        private bool isPause = false;
        private int currentVolume = 10;

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
            configData = ConfigManager.LoadPath();
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            smtc = mediaPlayer.SystemMediaTransportControls;
            updater = smtc.DisplayUpdater;

            InitializeComponent();
            InitPathData();
            InitVolume();
            InitSMTC();
        }

        private void InitPathData()
        {
            if (startPath == null || loopPath == null)
            {
                Trace.TraceWarning("startPath or loopPath is null. Init path again.");
                startPath = new OpenFileDialog();
                loopPath = new OpenFileDialog();
            }
            startPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            loopPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            if (configData != null)
            {
                // if pathData not null -> pathData is specified

                // then check pathData is valid path using File.Exists()
                // if path does not exist -> ignore setting up path
                if (File.Exists(configData.StartPath))
                {
                    StartField.Text = configData.StartPath; // show path in GUI
                    startPath.FileName = configData.StartPath; // set path in app logic
                }
                if (File.Exists(configData.LoopPath))
                {
                    LoopField.Text = configData.LoopPath;
                    loopPath.FileName = configData.LoopPath;
                }
            }
        }

        private void InitVolume()
        {
            if (configData == null)
                Trace.TraceError("InitVolume: config data is null");
            else if (configData.Volume == null)
                Trace.TraceInformation("InitVolume: configData doesn't have Volume value");
            else if (configData.Volume >= 0 && configData.Volume <= AppConstants.VOLUME_SCALE)
                currentVolume = (int)configData.Volume;
            else
                currentVolume = (int)AppConstants.VOLUME_SCALE;

            volValue.Text = currentVolume.ToString();
            AudioManager.SetVolume(currentVolume / AppConstants.VOLUME_SCALE);
        }

        private void InitSMTC()
        {
            if (mediaPlayer == null || smtc == null || updater == null)
                throw new NullReferenceException("Cannot initialize SystemMediaTransportControls, consider checking Windows version.");
            mediaPlayer.CommandManager.IsEnabled = false;
            smtc.IsPlayEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsStopEnabled = true;
            smtc.IsNextEnabled = false;
            smtc.IsPreviousEnabled = false;
            smtc.ButtonPressed += OnPlayPause;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Artist = "Someone in HyperGryph, idk :)";
            updater.MusicProperties.AlbumArtist = "Someone in HyperGryph, idk :)";
            updater.MusicProperties.Title = "Arknights BGM";
            // TODO: Thumbnail does not work properly, need to fix
            updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(Application.GetResourceStream(new Uri("img/schwarz.jpg", UriKind.Relative)).Stream.AsRandomAccessStream());
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
            smtc.IsEnabled = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;

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
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                updater.Update();
            }
            else
            {
                AudioManager.InitAudio();
                AudioManager.PlayBGM(startPath.FileName, loopPath.FileName);
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                updater.Update();
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
            smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            smtc.IsEnabled = false;
        }

        private void pause_Click(object sender, RoutedEventArgs? e)
        {
            isPause = true;
            AudioManager.PauseAudio();
            pause_button.IsEnabled = false;
            play_button.IsEnabled = true;
            stop_button.IsEnabled = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
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
        private void play_handler(object sender, EventArgs? e)
        {
            if (play_button.IsEnabled)
                play_Click(sender, null);
        }

        private void pause_handler(object sender, EventArgs? e)
        {
            if (pause_button.IsEnabled)
                pause_Click(sender, null);
        }

        private void stop_handler(object sender, EventArgs? e)
        {
            if (stop_button.IsEnabled)
                stop_Click(sender, null);
        }

        private void volDown_Click(object sender, RoutedEventArgs e)
        {
            var currentVol = Int32.Parse(volValue.Text);
            if (currentVol > 0)
            {
                currentVol--;
                volValue.Text = currentVol.ToString();
                AudioManager.SetVolume(currentVol / AppConstants.VOLUME_SCALE);
            }
            ConfigManager.SaveVolume(currentVol);
        }

        private void volUp_Click(object sender, RoutedEventArgs e)
        {
            var currentVol = Int32.Parse(volValue.Text);
            if (currentVol < AppConstants.VOLUME_SCALE)
            {
                currentVol++;
                volValue.Text = currentVol.ToString();
                AudioManager.SetVolume(currentVol / AppConstants.VOLUME_SCALE);
            }
            ConfigManager.SaveVolume(currentVol);
        }

        private void remove_intro_Click(object sender, RoutedEventArgs e)
        {
            if (AudioManager.IsStopped)
            {
                startPath.FileName = string.Empty;
                StartField.Text = string.Empty;
                ConfigManager.SavePath(startPath.FileName, loopPath.FileName);
            }
            else
            {
                MessageBox.Show("Stop music before remove file path");
            }
        }

        private void remove_loop_Click(object sender, RoutedEventArgs e)
        {
            if (AudioManager.IsStopped)
            {
                loopPath.FileName = string.Empty;
                LoopField.Text = string.Empty;
                ConfigManager.SavePath(startPath.FileName, loopPath.FileName);
            }
            else
            {
                MessageBox.Show("Stop music before remove file path");
            }
        }

        private void OnPlayPause(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Dispatcher.Invoke(() =>
                    {
                        play_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Dispatcher.Invoke(() =>
                    {
                        pause_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Dispatcher.Invoke(() =>
                    {
                        stop_handler(sender, null);
                    });
                    break;
                default:
                    Trace.TraceWarning("Incorrect input");
                    break;
            }
        }
    }
}
