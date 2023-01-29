using System;
using System.IO;
using System.Windows;
using System.Diagnostics;
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

        private bool isPause = false;
        private int currentVolume = 100;

        public MainWindow()
        {
            preferences = PreferencesHelper.LoadPreferences();
            dispatcherTimer = new DispatcherTimer();

            InitializeComponent();
            PathHelper.Init(preferences);
            PathHelper.TurnOnAutoUpdateGUI(IntroField, LoopField);
            InitVolume();
            SMTCHelper.InitSMTC(OnPlayPause);
            SMTCHelper.UpdateThumbnail();
            InitCheckbox();
            InitBackground();
            InitTimer();
        }

        #region Initialize
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
            PathHelper.AutoFill = autoFill.IsChecked ?? false;
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
            if (PathHelper.OpenIntroPathDialog() != null)
            {
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            SMTCHelper.Enable();
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            SMTCHelper.Disable();
            if (PathHelper.OpenLoopPathDialog() != null)
            {
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            SMTCHelper.Enable();
        }

        private void Play_Click(object sender, RoutedEventArgs? e)
        {
            // If both 2 two files is not set
            if (PathHelper.Intro == string.Empty && LoopField.Text == string.Empty)
            {
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
                    SMTCHelper.UpdateStatus(MediaPlaybackStatus.Playing);
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

            // If only one file is not set -> still play music but in loop mode.
            if (PathHelper.Intro == string.Empty || PathHelper.Loop == string.Empty)
            {
                // Check if start path is found -> PlayLoop start path
                // else -> PlayLoop loop path
                string filePath = PathHelper.Intro == string.Empty ? PathHelper.Intro : PathHelper.Loop;
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
                if (AudioManager.PlayLoop(filePath) == AudioManagerState.FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    TaskbarChangeToPlay();
                    return;
                }
            }
            else
            {
                if (AudioManager.PlayBGM(PathHelper.Intro, PathHelper.Loop) == AudioManagerState.FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    TaskbarChangeToPlay();
                    return;
                }
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            timer.Start();
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Playing);
        }

        private void Stop_Click(object? sender, RoutedEventArgs? e)
        {
            isPause = false;
            AudioManager.Stop();
            AllowChooseFile(true);
            play_button.IsEnabled = true;
            stop_button.IsEnabled = false;
            pause_button.IsEnabled = false;
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Stopped);
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
            SMTCHelper.UpdateStatus(MediaPlaybackStatus.Paused);
            TaskbarChangeToPlay();
            timer.Stop();
        }

        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (PathHelper.Intro == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                PathHelper.Intro = string.Empty;
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            else
            {
                MessageBox.Show("Stop music before removing music file.");
            }
        }

        private void RemoveLoop_Click(object sender, RoutedEventArgs e)
        {
            if (PathHelper.Loop == string.Empty) return;
            if (AudioManager.IsStopped)
            {
                PathHelper.Loop = string.Empty;
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
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
            PathHelper.AutoFill = true;
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            PreferencesHelper.SavePreferences(AutoFill: false);
            PathHelper.AutoFill = false;
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

        private void TaskbarChangeToPlay()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/play.png"));
        }

        private void TaskbarChangeToPause()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/pause.png"));
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
