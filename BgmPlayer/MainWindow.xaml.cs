using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Media;

namespace bgmPlayer
{
    public partial class MainWindow : Window
    {
        private readonly Preferences? preferences;
        private readonly Timer timer = Timer.Instance;
        private readonly DispatcherTimer dispatcherTimer;

        private bool allowControlBySMTC = false;
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

            UpdateAudioControlButton(AudioPlayer.CurrentState);
            AllowChooseFile(AudioPlayer.IsStopped);
            Title = SMTCHelper.Title ?? AppConstants.DEFAULT_MUSIC_TITLE;

            AudioPlayer.StateChanged += UpdateAudioControlButton;
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
            AudioPlayer.SetVolume(currentVolume / AppConstants.VOLUME_SCALE);
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
            allowControlBySMTC = false;
            if (PathHelper.OpenIntroPathDialog() != null)
            {
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            allowControlBySMTC = true;
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            allowControlBySMTC = false;
            if (PathHelper.OpenLoopPathDialog() != null)
            {
                SMTCHelper.UpdateTitle(PathHelper.Intro, PathHelper.Loop);
            }
            allowControlBySMTC = true;
        }

        private void Play_Click(object sender, RoutedEventArgs? e)
        {
            if (PathHelper.Intro == string.Empty && PathHelper.Loop == string.Empty)
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }

            AllowChooseFile(false);
            UpdateAudioControlButton(AudioState.PLAY);
            SMTCHelper.IsEnable = true;
            TaskbarChangeIconToPause();

            if (AudioPlayer.IsPause)
            {
                try
                {
                    AudioPlayer.Continue();
                    timer.Start();
                }
                catch (NAudio.MmException)
                {
                    MessageBox.Show(AppConstants.AUDIO_DEVICE_ERROR_MSG, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Stop_Click(null, null);
                }
                return;
            }

            if (AudioPlayer.PlayBGM(PathHelper.Intro, PathHelper.Loop) == AudioPlayerState.FAILED)
            {
                MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                Stop_Click(null, null);
                return;
            }

            timer.Start();
            allowControlBySMTC = true;
        }

        private void Stop_Click(object? sender, RoutedEventArgs? e)
        {
            AudioPlayer.Stop();
            AllowChooseFile(true);
            UpdateAudioControlButton(AudioState.STOP);
            TaskbarChangeIconToPlay();
            timer.Stop();
            timer.Reset();
        }

        private void Pause_Click(object sender, RoutedEventArgs? e)
        {
            AudioPlayer.Pause();
            UpdateAudioControlButton(AudioState.PAUSE);
            TaskbarChangeIconToPlay();
            timer.Stop();
        }

        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (PathHelper.Intro == string.Empty) return;
            if (AudioPlayer.IsStopped)
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
            if (AudioPlayer.IsStopped)
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
            if ((AudioPlayer.IsPause || AudioPlayer.IsStopped) && SMTCHelper.IsEnable)
            {
                TaskbarChangeIconToPause();
                Play_Click(sender, null);
            }
            else if (AudioPlayer.IsPlaying)
            {
                TaskbarChangeIconToPlay();
                Pause_Click(sender, null);
            }
            else return;
        }

        private void TaskbarStop_handler(object sender, EventArgs? e)
        {
            if (!AudioPlayer.IsStopped)
                Stop_Click(sender, null);
        }
        #endregion

        #region Event handler
        private void OnPlayPause(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            if (!allowControlBySMTC) return;
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
        #endregion

        #region Private helper methods
        private void AllowChooseFile(bool isAllow)
        {
            intro_button.IsEnabled = isAllow;
            loop_button.IsEnabled = isAllow;
        }

        private void TaskbarChangeIconToPlay()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/play.png"));
        }

        private void TaskbarChangeIconToPause()
        {
            play_pause_taskbar.ImageSource = new BitmapImage(new Uri("pack://application:,,,/img/pause.png"));
        }

        private void SetVolume(int Volume)
        {
            currentVolume = Volume;
            VolSlider.Value = Volume;
            VolValue.Text = Volume.ToString();
            AudioPlayer.SetVolume(Volume / AppConstants.VOLUME_SCALE);
            PreferencesHelper.SavePreferences(Volume: Volume);
        }
        private void UpdateAudioControlButton(AudioState audioState)
        {
            switch (audioState)
            {
                case AudioState.PLAY:
                    play_button.IsEnabled = false;
                    stop_button.IsEnabled = true;
                    pause_button.IsEnabled = true;
                    break;
                case AudioState.PAUSE:
                    pause_button.IsEnabled = false;
                    play_button.IsEnabled = true;
                    stop_button.IsEnabled = true;
                    break;
                case AudioState.STOP:
                    play_button.IsEnabled = true;
                    stop_button.IsEnabled = false;
                    pause_button.IsEnabled = false;
                    break;
            }
        }
        #endregion
    }
}
