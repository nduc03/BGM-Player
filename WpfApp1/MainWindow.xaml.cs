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
        private OpenFileDialog introPath;
        private OpenFileDialog loopPath;
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

            introPath = new OpenFileDialog();
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
            if (introPath == null || loopPath == null)
            {
                Debug.WriteLine("InitPathData error: startPath or loopPath is null. Init path again.");
                introPath = new OpenFileDialog();
                loopPath = new OpenFileDialog();
            }
            introPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            loopPath.Filter = "Audio files (*.mp3, *.wav)|*.mp3; *.wav";
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            if (configData != null)
            {
                // if configData not null -> configData is specified
                // then check configData has valid path using File.Exists()
                // if path does not exist -> ignore setting up path
                if (File.Exists(configData.IntroPath))
                {
                    IntroField.Text = configData.IntroPath; // show path in GUI
                    introPath.FileName = configData.IntroPath; // set path in app logic
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
                Debug.WriteLine("InitVolume: config data is null, set volume to default value.");
            else if (configData.Volume == null)
                Debug.WriteLine("InitVolume: configData doesn't have Volume value");
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
            updater.MusicProperties.Title = "BGM Player";
            // TODO: Thumbnail does not work properly, need to fix
            updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(Application.GetResourceStream(new Uri("img/schwarz.jpg", UriKind.Relative)).Stream.AsRandomAccessStream());
            updater.Update();
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (introPath.ShowDialog() == true)
            {
                IntroField.Text = introPath.FileName;
                ConfigManager.SavePath(introPath.FileName, null);
                smtc.IsEnabled = true;
            }
        }

        private void loop_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (loopPath.ShowDialog() == true)
            {
                LoopField.Text = loopPath.FileName;
                ConfigManager.SavePath(null, loopPath.FileName);
                smtc.IsEnabled = true;
            }
        }

        private void play_Click(object sender, RoutedEventArgs? e)
        {
            if (!File.Exists(introPath.FileName) && !File.Exists(loopPath.FileName))
            {
                // If both 2 two files is not found or not set -> show error message.
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }

            AllowChooseFile(false);
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

            // If only one file is not found or not set -> still play music but in loop mode.
            if (!File.Exists(introPath.FileName) || !File.Exists(loopPath.FileName))
            {
                // Check if start path is found -> PlayLoop start path
                // else -> PlayLoop loop path
                string filePath = File.Exists(introPath.FileName) ? introPath.FileName : loopPath.FileName;
                updater.MusicProperties.Title = Path.GetFileNameWithoutExtension(filePath);
                AudioManager.InitAudio();
                AudioManager.PlayLoop(filePath);
                updater.Update();
            }
            else
            {
                updater.MusicProperties.Title = GetArknightsBgmFileName(introPath.FileName, loopPath.FileName) ?? "BGM Player";
                AudioManager.InitAudio();
                AudioManager.PlayBGM(introPath.FileName, loopPath.FileName);
                updater.Update();
            }
        }

        private void stop_Click(object sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.StopAudio();
            AllowChooseFile(true);
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

        private void volDown_Click(object sender, RoutedEventArgs e)
        {
            var currentVol = int.Parse(volValue.Text);
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
            var currentVol = int.Parse(volValue.Text);
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
                introPath.FileName = string.Empty;
                IntroField.Text = string.Empty;
                ConfigManager.SavePath(introPath.FileName, loopPath.FileName);
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
                ConfigManager.SavePath(introPath.FileName, loopPath.FileName);
            }
            else
            {
                MessageBox.Show("Stop music before remove file path");
            }
        }

        private void taskbar_play_handler(object sender, EventArgs? e)
        {
            if (play_button.IsEnabled && smtc.IsEnabled)
                play_Click(sender, null);
        }

        private void taskbar_stop_handler(object sender, EventArgs? e)
        {
            if (stop_button.IsEnabled)
                stop_Click(sender, null);
        }

        private void taskbar_pause_handler(object sender, EventArgs? e)
        {
            if (pause_button.IsEnabled)
                pause_Click(sender, null);
        }

        private void OnPlayPause(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Dispatcher.Invoke(() =>
                    {
                        taskbar_play_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Dispatcher.Invoke(() =>
                    {
                        taskbar_pause_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Dispatcher.Invoke(() =>
                    {
                        taskbar_stop_handler(sender, null);
                    });
                    break;
                default:
                    Trace.TraceWarning("Incorrect input");
                    break;
            }
        }

        private void AllowChooseFile(bool isAllow)
        {
            start.IsEnabled = isAllow;
            loop.IsEnabled = isAllow;
        }

        /// <summary>
        /// Get BGM name of intro and loop file.
        /// Only work correctly with Arknights music file pattern
        /// </summary>
        /// <param name="path1">Full absolute path to intro or loop file</param>
        /// <param name="path2">Full absolute path to intro or loop file</param>
        /// <returns>If correct pattern return BGM name. Return null when function cannot find the pattern</returns>
        private static string? GetArknightsBgmFileName(string path1, string path2)
        {
            string _path1 = Path.GetFileNameWithoutExtension(path1);
            string _path2 = Path.GetFileNameWithoutExtension(path2);
            if ((_path1.EndsWith("_intro") && _path2.EndsWith("_loop")) || (_path2.EndsWith("_intro") && _path1.EndsWith("_loop")))
            {
                if (_path1[.._path1.LastIndexOf('_')] == _path2[.._path2.LastIndexOf('_')])
                    return _path1[.._path1.LastIndexOf('_')];
                return null;
            }
            return null;
        }
    }
}
