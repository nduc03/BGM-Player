using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using Windows.Media;
using Windows.Storage.Streams;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace bgmPlayer
{
    public partial class MainWindow : Window
    {
        private readonly Preferences? preferences;
        private readonly Windows.Media.Playback.MediaPlayer mediaPlayer;
        private readonly SystemMediaTransportControls smtc;
        private readonly SystemMediaTransportControlsDisplayUpdater updater;
        private readonly Timer timer = Timer.Instance;
        private readonly DispatcherTimer dispatcherTimer;
        private OpenFileDialog IntroPath;
        private OpenFileDialog LoopPath;

        private bool isPause = false;
        private int currentVolume = 100;
        private object? keepThumbnailOpen = null;

        public MainWindow()
        {
            IntroPath = new OpenFileDialog();
            LoopPath = new OpenFileDialog();
            preferences = PreferencesHelper.LoadPreferences();
            mediaPlayer = new Windows.Media.Playback.MediaPlayer();
            smtc = mediaPlayer.SystemMediaTransportControls;
            updater = smtc.DisplayUpdater;
            dispatcherTimer = new DispatcherTimer();

            InitializeComponent();
            InitPathData();
            InitVolume();
            InitSMTC();
            InitCheckbox();
            InitBackground();
            InitTimer();
        }

        #region Initialize
        private void InitPathData()
        {
            if (IntroPath == null || LoopPath == null)
            {
                Debug.WriteLine("InitPathData error: startPath or loopPath is null. Init path again.");
                IntroPath = new OpenFileDialog();
                LoopPath = new OpenFileDialog();
            }
            IntroPath.Filter = "Wave sound|*.wav|MP3 (not recommended)|*.mp3";
            LoopPath.Filter = "Wave sound|*.wav|MP3 (not recommended)|*.mp3";
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            if (preferences != null)
            {
                // if configData not null -> configData is specified
                // then check configData has valid path using File.Exists()
                // if path does not exist -> ignore setting up path
                if (File.Exists(preferences.IntroPath))
                {
                    IntroField.Text = preferences.IntroPath; // show path in GUI
                    IntroPath.FileName = preferences.IntroPath; // set path in app logic
                }
                if (File.Exists(preferences.LoopPath))
                {
                    LoopField.Text = preferences.LoopPath;
                    LoopPath.FileName = preferences.LoopPath;
                }
            }
        }

        private void InitVolume()
        {
            if (preferences == null)
                Debug.WriteLine("InitVolume: config data is null, set volume to default value.");
            else if (preferences.Volume == null)
                Debug.WriteLine("InitVolume: configData doesn't have Volume value");
            else if (preferences.Volume >= 0 && preferences.Volume <= AppConstants.VOLUME_SCALE)
                currentVolume = (int)preferences.Volume;
            else
                currentVolume = (int)AppConstants.VOLUME_SCALE;

            VolSlider.Value = currentVolume;
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
            UpdateThumbnail();
            UpdateTitle();
            smtc.IsEnabled = true;
        }

        private void InitCheckbox()
        {
            autoFill.Checked += OnCheck;
            autoFill.Unchecked += OnUnchecked;
            var config = PreferencesHelper.LoadPreferences();
            if (config != null && config.AutoFill != null)
                autoFill.IsChecked = config.AutoFill;
            else
            {
                autoFill.IsChecked = false;
                PreferencesHelper.SavePreferences(AutoFill: false);
            }
        }

        private void InitBackground()
        {
#if ME
            System.Windows.Media.ImageBrush background = new(new BitmapImage(new Uri("pack://application:,,,/img/schwarz_blured.png")))
            {
                Stretch = System.Windows.Media.Stretch.UniformToFill
            };
            Background = background;
#endif
        }

        private void InitTimer()
        {
            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.5);
            dispatcherTimer.Tick += (o, e) =>
            {
                TimerBlock.Text = "Played:  " + timer.GetParsedElapsed();
            };
            dispatcherTimer.Start();

            StateChanged += (o, e) =>
            {
                if (WindowState == WindowState.Minimized) dispatcherTimer.Stop();
                else dispatcherTimer.Start();
            };
        }
        #endregion

        #region Button handler
        private void Intro_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (IntroPath.ShowDialog() == true)
            {
                SetIntroPath(IntroPath.FileName);
                TryAutoSetLoop();
                UpdateTitle();
                smtc.IsEnabled = true;
                return;
            }
            smtc.IsEnabled = true;
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            smtc.IsEnabled = false;
            if (LoopPath.ShowDialog() == true)
            {
                SetLoopPath(LoopPath.FileName);
                TryAutoSetIntro();
                UpdateTitle();
                smtc.IsEnabled = true;
                return;
            }
            smtc.IsEnabled = true;
        }

        private void Play_Click(object sender, RoutedEventArgs? e)
        {
            if (!File.Exists(IntroField.Text) && !File.Exists(LoopField.Text))
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
                try
                {
                    AudioManager.Continue();
                    smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    timer.Start();
                }
                catch (NAudio.MmException)
                {
                    if (MessageBox.Show(
                            "Some problem with audio devices or drivers, the music cannot be played, consider restart this app or computer.\n" +
                                "Choose 'Yes' to restart the app or 'No' to stop the music.",
                            "Error!",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Error
                        ) == MessageBoxResult.Yes)
                    {
                        Process.Start(Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().Location));
                        Application.Current.Shutdown();
                    }
                    else
                        Stop_Click(null, null);
                }
                return;
            }

            // If only one file is not found or not set -> still play music but in loop mode.
            if (!File.Exists(IntroField.Text) || !File.Exists(LoopField.Text))
            {
                Mp3Check();
                // Check if start path is found -> PlayLoop start path
                // else -> PlayLoop loop path
                string filePath = File.Exists(IntroField.Text) ? IntroField.Text : LoopField.Text;
                updater.MusicProperties.Title = Path.GetFileNameWithoutExtension(filePath);
                if (AudioManager.PlayLoop(filePath) == AudioManagerState.FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    TaskbarChangeToPlay();
                    return;
                }
            }
            else
            {
                Mp3Check();
                if (AudioManager.PlayBGM(IntroField.Text, LoopField.Text) == AudioManagerState.FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    TaskbarChangeToPlay();
                    return;
                }
                updater.MusicProperties.Title = GetBgmFileName(IntroField.Text, LoopField.Text) ?? "BGM Player";
            }

            timer.Start();
            smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
            updater.Update();
        }

        private void Stop_Click(object? sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.Stop();
            AllowChooseFile(true);
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
            TaskbarChangeToPlay();
            timer.Stop();
            timer.Reset();
        }

        private void Pause_Click(object sender, RoutedEventArgs? e)
        {
            isPause = true;
            AudioManager.Pause();
            pause_button.IsEnabled = false;
            play_button.IsEnabled = true;
            stop_button.IsEnabled = true;
            smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
            TaskbarChangeToPlay();
            timer.Stop();
        }

        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (IntroPath.FileName == string.Empty && IntroField.Text == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                SetIntroPath(string.Empty);
                UpdateTitle();
                if (LoopField.Text == string.Empty) smtc.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Stop music before removing music file.");
            }
        }

        private void RemoveLoop_Click(object sender, RoutedEventArgs e)
        {
            if (LoopPath.FileName == string.Empty && LoopField.Text == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                SetLoopPath(string.Empty);
                UpdateTitle();
                if (IntroField.Text == string.Empty) smtc.IsEnabled = false;
            }
            else
            {
                MessageBox.Show("Stop music before removing music file.");
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
            PreferencesHelper.SavePreferences(AutoFill: true);
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            PreferencesHelper.SavePreferences(AutoFill: false);
        }

        private void VolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolume((float)VolSlider.Value);
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            currentVolume = Math.Clamp(currentVolume + (e.Delta / AppConstants.MOUSE_WHEEL_SCALE), 0, 100);
            if (currentVolume != (int)AudioManager.GetVolume())
            {
                AudioManager.SetVolume(currentVolume);
                VolSlider.Value = currentVolume;
            }
        }
        #endregion

        #region Private helper methods
        private void AllowChooseFile(bool isAllow)
        {
            intro_button.IsEnabled = isAllow;
            loop_button.IsEnabled = isAllow;
        }

        private void Mp3Check()
        {
            if (Path.GetExtension(IntroField.Text) == ".mp3" || Path.GetExtension(LoopField.Text) == ".mp3")
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
            string introPath = IntroField.Text;

            if (!Path.GetFileNameWithoutExtension(introPath).EndsWith("_intro")) return;

            string expectedLoopPath = introPath[..introPath.LastIndexOf('_')] + AppConstants.LOOP_END + Path.GetExtension(introPath);

            if (File.Exists(expectedLoopPath))
            {
                SetLoopPath(expectedLoopPath);
            }
        }

        private void TryAutoSetIntro()
        {
            if (autoFill == null) return;
            if (autoFill.IsChecked == false || autoFill.IsChecked == null) return;
            string loopPath = LoopField.Text;

            if (!Path.GetFileNameWithoutExtension(loopPath).EndsWith("_loop")) return;

            string expectedIntroPath = loopPath[..loopPath.LastIndexOf('_')] + AppConstants.INTRO_END + Path.GetExtension(loopPath);

            if (File.Exists(expectedIntroPath))
            {
                SetIntroPath(expectedIntroPath);
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

        private void SetIntroPath(string introPath)
        {
            IntroField.Text = introPath;
            IntroPath.FileName = introPath;
            PreferencesHelper.SavePreferences(IntroPath: introPath);
        }

        private void SetLoopPath(string loopPath)
        {
            LoopField.Text = loopPath;
            LoopPath.FileName = loopPath;
            PreferencesHelper.SavePreferences(LoopPath: loopPath);
        }

        private void SetVolume(float Volume)
        {
            VolSlider.Value = Volume;
            VolValue.Text = ((int)Volume).ToString();
            AudioManager.SetVolume(Volume / AppConstants.VOLUME_SCALE);
            PreferencesHelper.SavePreferences(Volume: Volume);
        }

        private void UpdateTitle()
        {
            string? title = GetBgmFileName(IntroField.Text, LoopField.Text);
            string artist = string.Empty;
            string defaultTitle = "BGM Player";
            if (title == null)
            {
                if (IntroField.Text != string.Empty && LoopField.Text == string.Empty)
                    title = Path.GetFileNameWithoutExtension(IntroField.Text);
                else if (LoopField.Text != string.Empty && IntroField.Text == string.Empty)
                    title = Path.GetFileNameWithoutExtension(LoopField.Text);
                else title = defaultTitle;
            }
            else artist = "Monster Siren Records";
            updater.MusicProperties.Title = title ?? defaultTitle;
            updater.MusicProperties.Artist = artist ?? string.Empty;
            Application.Current.MainWindow.Title = title ?? defaultTitle;
            updater.Update();
        }

        private async void UpdateThumbnail()
        {
            // Create temp file as a workaround since creating thumbnail
            // from RandomAccessStreamReference.CreateFromStream does not work
            if (!File.Exists($"{AppConstants.CACHE_FOLDER}/thumbnail.jpg"))
            {
                Directory.CreateDirectory(AppConstants.CACHE_FOLDER).Attributes = FileAttributes.Hidden;
                using var file = File.Create($"{AppConstants.CACHE_FOLDER}/thumbnail.jpg");
                var stream = Application.GetResourceStream(new Uri("img/schwarz.jpg", UriKind.Relative)).Stream;
                stream.CopyTo(file);
            }
            keepThumbnailOpen = File.Open($"{AppConstants.CACHE_FOLDER}/thumbnail.jpg", FileMode.Open);
            updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(
                await Windows.Storage.StorageFile.GetFileFromPathAsync(
                        AppDomain.CurrentDomain.BaseDirectory + $"{AppConstants.CACHE_FOLDER}\\thumbnail.jpg"
                    )
            );
        }

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
            string p1 = Path.GetFileNameWithoutExtension(path1);
            string p2 = Path.GetFileNameWithoutExtension(path2);
            if ((p1.EndsWith(AppConstants.INTRO_END) && p2.EndsWith(AppConstants.LOOP_END)) || (p2.EndsWith(AppConstants.INTRO_END) && p1.EndsWith(AppConstants.LOOP_END)))
            {
                if (p1[..p1.LastIndexOf(AppConstants.INTRO_END)] == p2[..p2.LastIndexOf(AppConstants.LOOP_END)])
                    return p1[..p1.LastIndexOf(AppConstants.INTRO_END)];
                return null;
            }
            return null;
        }
        #endregion

    }
}
