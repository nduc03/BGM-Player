using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using Windows.Media;
using Windows.Storage.Streams;
using System.Windows.Media.Imaging;

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
            configData = ConfigManager.LoadConfig();
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            smtc = mediaPlayer.SystemMediaTransportControls;
            updater = smtc.DisplayUpdater;

            InitializeComponent();
            InitPathData();
            InitVolume();
            InitSMTC();
            InitCheckbox();
        }

        #region Initialize
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
            smtc.IsEnabled = true;
        }

        private void InitCheckbox()
        {
            autoFill.Checked += OnCheck;
            autoFill.Unchecked += OnUnchecked;
            var config = ConfigManager.LoadConfig();
            if (config != null)
                autoFill.IsChecked = config.AutoFill;
        }
        #endregion

        #region Button handler
        private void Intro_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (introPath.ShowDialog() == true)
            {
                IntroField.Text = introPath.FileName;
                TryAutoSetLoop();
                ConfigManager.SaveConfig(IntroPath: introPath.FileName);
                smtc.IsEnabled = false;
                return;
            }
            smtc.IsEnabled = true;
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (loopPath.ShowDialog() == true)
            {
                LoopField.Text = loopPath.FileName;
                TryAutoSetIntro();
                ConfigManager.SaveConfig(LoopPath: loopPath.FileName);
                smtc.IsEnabled = false;
                return;
            }
            smtc.IsEnabled = true;
        }

        private void Play_Click(object sender, RoutedEventArgs? e)
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
            TaskbarChangeToPause();

            if (isPause)
            {
                isPause = false;
                AudioManager.ContinueAudio();
                smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                return;
            }

            // If only one file is not found or not set -> still play music but in loop mode.
            if (!File.Exists(introPath.FileName) || !File.Exists(loopPath.FileName))
            {
                Mp3Check();
                // Check if start path is found -> PlayLoop start path
                // else -> PlayLoop loop path
                string filePath = File.Exists(introPath.FileName) ? introPath.FileName : loopPath.FileName;
                updater.MusicProperties.Title = Path.GetFileNameWithoutExtension(filePath);
                AudioManager.InitAudio();
                if (AudioManager.PlayLoop(filePath) == AudioManagerState.PLAY_FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                Mp3Check();
                AudioManager.InitAudio();
                if (AudioManager.PlayBGM(introPath.FileName, loopPath.FileName) == AudioManagerState.PLAY_FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                updater.MusicProperties.Title = GetBgmFileName(introPath.FileName, loopPath.FileName) ?? "BGM Player";
            }

            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            updater.Update();
        }

        private void Stop_Click(object sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.StopAudio();
            AllowChooseFile(true);
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            TaskbarChangeToPlay();
        }

        private void Pause_Click(object sender, RoutedEventArgs? e)
        {
            isPause = true;
            AudioManager.PauseAudio();
            pause_button.IsEnabled = false;
            play_button.IsEnabled = true;
            stop_button.IsEnabled = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            TaskbarChangeToPlay();
        }

        private void VolDown_Click(object sender, RoutedEventArgs e)
        {
            var currentVol = int.Parse(volValue.Text);
            if (currentVol > 0)
            {
                currentVol--;
                volValue.Text = currentVol.ToString();
                AudioManager.SetVolume(currentVol / AppConstants.VOLUME_SCALE);
                ConfigManager.SaveConfig(Volume: currentVol);
            }
        }

        private void VolUp_Click(object sender, RoutedEventArgs e)
        {
            var currentVol = int.Parse(volValue.Text);
            if (currentVol < AppConstants.VOLUME_SCALE)
            {
                currentVol++;
                volValue.Text = currentVol.ToString();
                AudioManager.SetVolume(currentVol / AppConstants.VOLUME_SCALE);
                ConfigManager.SaveConfig(Volume: currentVol);
            }
        }

        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (loopPath.FileName == string.Empty && LoopField.Text == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                introPath.FileName = string.Empty;
                IntroField.Text = string.Empty;
                ConfigManager.SaveConfig(IntroPath: introPath.FileName);
                smtc.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Stop music before removing file path");
            }
        }

        private void RemoveLoop_Click(object sender, RoutedEventArgs e)
        {
            if (loopPath.FileName == string.Empty && LoopField.Text == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                loopPath.FileName = string.Empty;
                LoopField.Text = string.Empty;
                ConfigManager.SaveConfig(LoopPath: loopPath.FileName);
                smtc.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Stop music before removing file path");
            }
        }
        #endregion

        #region Taskbar handler
        private void TaskbarPlayPause_handler(object sender, EventArgs? e)
        {
            if (play_button.IsEnabled && smtc.IsEnabled)
            {
                TaskbarChangeToPause();
                Play_Click(sender, null);
            }
            else if (pause_button.IsEnabled)
            {
                TaskbarChangeToPlay();
                Pause_Click(sender, null);
            }
            else
            {
                return;
            }
        }

        private void TaskbarStop_handler(object sender, EventArgs? e)
        {
            if (stop_button.IsEnabled)
                Stop_Click(sender, null);
        }
        #endregion

        #region Event handler
        private void OnPlayPause(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Dispatcher.Invoke(() =>
                    {
                        TaskbarPlayPause_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Dispatcher.Invoke(() =>
                    {
                        TaskbarPlayPause_handler(sender, null);
                    });
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Dispatcher.Invoke(() =>
                    {
                        TaskbarStop_handler(sender, null);
                    });
                    break;
                default:
                    Trace.TraceWarning("Incorrect input");
                    break;
            }
        }

        private void OnCheck(object sender, RoutedEventArgs e)
        {
            ConfigManager.SaveConfig(AutoFill: true);
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            ConfigManager.SaveConfig(AutoFill: false);
        }
        #endregion

        #region Private helper methods
        private void AllowChooseFile(bool isAllow)
        {
            intro.IsEnabled = isAllow;
            loop.IsEnabled = isAllow;
        }

        private void Mp3Check()
        {
            if (Path.GetExtension(introPath.FileName) == ".mp3" || Path.GetExtension(loopPath.FileName) == ".mp3")
                MessageBox.Show(
                    "You are using compressed file mp3, which is not recommended for BGM playback.\n" +
                        "Consider convert the file to .wav for smoother experience.",
                    "Warning!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
        }

        private void TryAutoSetLoop()
        {
            if (autoFill == null) return;
            if (autoFill.IsChecked == false || autoFill.IsChecked == null) return;
            string _introPath = introPath.FileName;

            if (!Path.GetFileNameWithoutExtension(_introPath).EndsWith("_intro")) return;

            string shouldLoopPath = _introPath[.._introPath.LastIndexOf('_')] + AppConstants.LOOP_END + Path.GetExtension(_introPath);

            if (File.Exists(shouldLoopPath))
            {
                loopPath.FileName = shouldLoopPath;
                LoopField.Text = shouldLoopPath;
                ConfigManager.SaveConfig(LoopPath: shouldLoopPath);
            }
        }

        private void TryAutoSetIntro()
        {
            if (autoFill == null) return;
            if (autoFill.IsChecked == false || autoFill.IsChecked == null) return;
            string _loopPath = loopPath.FileName;

            if (!Path.GetFileNameWithoutExtension(_loopPath).EndsWith("_loop")) return;

            string shouldIntroPath = _loopPath[.._loopPath.LastIndexOf('_')] + AppConstants.INTRO_END + Path.GetExtension(_loopPath);

            if (File.Exists(shouldIntroPath))
            {
                introPath.FileName = shouldIntroPath;
                IntroField.Text = shouldIntroPath;
                ConfigManager.SaveConfig(IntroPath: shouldIntroPath);
            }
        }

        private void TaskbarChangeToPlay()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/play.png"));
        }

        private void TaskbarChangeToPause()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/pause.png"));
        }
        #endregion

        /// <summary>
        /// Get BGM name of intro and loop file.
        /// Only work correctly with correct pattern.
        /// Pattern: file_name_intro, file_name_loop
        /// </summary>
        /// <param name="path1">Full absolute path to intro or loop file</param>
        /// <param name="path2">Full absolute path to intro or loop file</param>
        /// <returns>If correct pattern return BGM name. Return null when function cannot find the pattern</returns>
        private static string? GetBgmFileName(string path1, string path2)
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
