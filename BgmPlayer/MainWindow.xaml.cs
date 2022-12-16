using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Win32;
using Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace bgmPlayer
{
    public partial class MainWindow : Window
    {
        private readonly Preferences? preferences;
        private readonly Timer timer = Timer.Instance;
        private readonly DispatcherTimer dispatcherTimer;
        private OpenFileDialog IntroPath;
        private OpenFileDialog LoopPath;

        private bool isPause = false;
        private int currentVolume = 100;

        public MainWindow()
        {
            IntroPath = new OpenFileDialog();
            LoopPath = new OpenFileDialog();
            preferences = PreferencesHelper.LoadPreferences();
            dispatcherTimer = new DispatcherTimer();

            InitializeComponent();
            InitPathData();
            InitVolume();
            SMTCHelper.InitSMTC(OnPlayPause);
            SMTCHelper.UpdateThumbnail();
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

        private void InitCheckbox()
        {
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
            SMTCHelper.Disable();
            if (IntroPath.ShowDialog() == true)
            {
                SetIntroPath(IntroPath.FileName);
                TryAutoSetLoop();
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
                SMTCHelper.Enable();
                return;
            }
            SMTCHelper.Enable();
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            SMTCHelper.Disable();
            if (LoopPath.ShowDialog() == true)
            {
                SetLoopPath(LoopPath.FileName);
                TryAutoSetIntro();
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
                SMTCHelper.Enable();
                return;
            }
            SMTCHelper.Enable();
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
            SMTCHelper.Enable();
            TaskbarChangeToPause();

            if (isPause)
            {
                isPause = false;
                try
                {
                    AudioManager.Continue();
                    SMTCHelper.UpdateState(MediaPlaybackStatus.Playing);
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
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
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
                //updater.MusicProperties.Title = Utils.GetBgmFileName(IntroField.Text, LoopField.Text) ?? "BGM Player";
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
            }

            timer.Start();
            SMTCHelper.UpdateState(MediaPlaybackStatus.Playing);
        }

        private void Stop_Click(object? sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.Stop();
            AllowChooseFile(true);
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            SMTCHelper.UpdateState(MediaPlaybackStatus.Stopped);
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
            SMTCHelper.UpdateState(MediaPlaybackStatus.Paused);
            TaskbarChangeToPlay();
            timer.Stop();
        }

        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (IntroPath.FileName == string.Empty && IntroField.Text == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                SetIntroPath(string.Empty);
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
                if (LoopField.Text == string.Empty) SMTCHelper.Disable();
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
                SMTCHelper.UpdateTitle(IntroField.Text, LoopField.Text);
                if (IntroField.Text == string.Empty) SMTCHelper.Disable();
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
            if (play_button.IsEnabled && SMTCHelper.IsSmtcEnable)
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

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            PreferencesHelper.SavePreferences(AutoFill: true);
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            PreferencesHelper.SavePreferences(AutoFill: false);
        }

        private void VolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolume((int)VolSlider.Value);
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            SetVolume(Math.Clamp(currentVolume + (e.Delta / AppConstants.MOUSE_WHEEL_SCALE), 0, 100));
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            FileHelper.InstantSave();
            base.OnClosing(e);
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

        private void SetVolume(int Volume)
        {
            currentVolume = Volume;
            VolSlider.Value = Volume;
            VolValue.Text = Volume.ToString();
            AudioManager.SetVolume(Volume / AppConstants.VOLUME_SCALE);
            PreferencesHelper.SavePreferences(Volume: Volume);
        }
        #endregion
    }
}
