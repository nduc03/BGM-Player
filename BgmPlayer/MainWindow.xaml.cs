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
        private readonly Timer timer = Timer.Instance;
        private readonly DispatcherTimer dispatcherTimer;

        private bool allowControlBySMTC = false;
        private int currentVolume = 100;

        public MainWindow()
        {
            dispatcherTimer = new DispatcherTimer();

            InitializeComponent();
            AudioPathManager.Init(PersistedStateManager.LoadState() ?? new PersistedState());
            AudioPathManager.InitTextBlock(IntroField, LoopField);
            SMTCManager.InitSMTC(OnPlayPause);
            SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            SMTCManager.UpdateThumbnail();
            InitVolume();
            InitCheckbox();
            InitBackgroundImage();
            InitTimer();

            UpdateAudioControlButton(AudioPlayer.CurrentState);
            AllowChooseFile(AudioPlayer.IsStopped);
            Title = SMTCManager.WindowTitle ?? SMTCManager.Title ?? AppConstants.DEFAULT_MUSIC_TITLE;

            AudioPlayer.StateChanged += UpdateAudioControlButton;
        }

        #region Initialize
        private void InitVolume()
        {
            var state = PersistedStateManager.LoadState();
            if (state == null)
                Debug.WriteLine("InitVolume: config data is null, set volume to default value.");
            else if (state.Volume == null)
                Debug.WriteLine("InitVolume: configData doesn't have Volume value");
            else if (state.Volume >= 0 && state.Volume <= AppConstants.VOLUME_SCALE)
                currentVolume = (int)state.Volume;
            else
                currentVolume = (int)AppConstants.VOLUME_SCALE;

            VolSlider.Value = currentVolume;
            AudioPlayer.SetVolume(currentVolume / AppConstants.VOLUME_SCALE);
        }

        private void InitCheckbox()
        {
            var state = PersistedStateManager.LoadState();
            if (state != null && state.AutoFill != null)
                autoFill.IsChecked = state.AutoFill;
            else
            {
                autoFill.IsChecked = false;
                PersistedStateManager.SaveState(AutoFill: false);
            }
            AudioPathManager.AutoFill = autoFill.IsChecked ?? false;
        }

        private void InitBackgroundImage()
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
            if (AudioPathManager.OpenIntroPathDialog() != null)
            {
                SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            }
            allowControlBySMTC = true;
        }

        private void Loop_Click(object sender, RoutedEventArgs e)
        {
            allowControlBySMTC = false;
            if (AudioPathManager.OpenLoopPathDialog() != null)
            {
                SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            }
            allowControlBySMTC = true;
        }

        private void PlayPause_Click(object? sender, RoutedEventArgs? e)
        {
            if (AudioPathManager.Intro == string.Empty && AudioPathManager.Loop == string.Empty)
            {
                MessageBox.Show(AppConstants.FILE_MISSING, AppConstants.USER_ERROR_TITLE);
                return;
            }
            if (AudioPlayer.IsStopped)
            {
                AllowChooseFile(false);
                UpdateAudioControlButton(AudioState.PLAY);
                allowControlBySMTC = true;
                TaskbarChangeIconToPause();
                timer.Start();
                
                if (AudioPlayer.PlayBGM(AudioPathManager.Intro, AudioPathManager.Loop) == AudioPlayerState.FAILED)
                {
                    MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Stop_Click(null, null);
                    return;
                }
                return;
            }

            if (AudioPlayer.IsPause)
            {
                try
                {
                    AudioPlayer.Continue();
                    UpdateAudioControlButton(AudioState.PLAY);
                    timer.Start();
                }
                catch (NAudio.MmException)
                {
                    MessageBox.Show(AppConstants.AUDIO_DEVICE_ERROR_MSG, "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Stop_Click(null, null);
                }
                return;
            }

            if (AudioPlayer.IsPlaying)
            {
                AudioPlayer.Pause();
                UpdateAudioControlButton(AudioState.PAUSE);
                TaskbarChangeIconToPlay();
                timer.Stop();
                return;
            }
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


        private void RemoveIntro_Click(object sender, RoutedEventArgs e)
        {
            if (AudioPathManager.Intro == string.Empty) return;
            if (AudioPlayer.IsStopped)
            {
                AudioPathManager.Intro = string.Empty;
                SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            }
            else
            {
                MessageBox.Show("Stop music before removing music file.");
            }
        }

        private void RemoveLoop_Click(object sender, RoutedEventArgs e)
        {
            if (AudioPathManager.Loop == string.Empty) return;
            if (AudioPlayer.IsStopped)
            {
                AudioPathManager.Loop = string.Empty;
                SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            }
            else
            {
                MessageBox.Show("Stop music before removing music file.");
            }
        }
        #endregion

        #region Taskbar handler
        private void TaskbarPlayPause_handler(object? sender, EventArgs? e)
        {
            if ((AudioPlayer.IsPause || AudioPlayer.IsStopped) && SMTCManager.IsEnable)
            {
                TaskbarChangeIconToPause();
                PlayPause_Click(sender, null);
            }
            else if (AudioPlayer.IsPlaying)
            {
                TaskbarChangeIconToPlay();
                PlayPause_Click(sender, null);
            }
            else return;
        }

        private void TaskbarStop_handler(object? sender, EventArgs? e)
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
                    break;
            }
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            PersistedStateManager.SaveState(AutoFill: true);
            AudioPathManager.AutoFill = true;
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            PersistedStateManager.SaveState(AutoFill: false);
            AudioPathManager.AutoFill = false;
        }

        private void VolSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetVolume((int)VolSlider.Value);
        }

        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            SetVolume(Math.Clamp(currentVolume + (e.Delta / AppConstants.MOUSE_WHEEL_SCALE), 0, 100));
        }
#if DEBUG
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            GC.Collect();
        }
#endif
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
            PersistedStateManager.SaveState(Volume: Volume);
        }
        private void UpdateAudioControlButton(AudioState audioState)
        {
            switch (audioState)
            {
                case AudioState.PLAY:
                    play_pause_button.Content = "Pause";
                    TaskbarChangeIconToPause();
                    stop_button.IsEnabled = true;
                    break;
                case AudioState.PAUSE:
                    play_pause_button.Content = "Play";
                    TaskbarChangeIconToPlay();
                    stop_button.IsEnabled = true;
                    break;
                case AudioState.STOP:
                    play_pause_button.Content = "Play";
                    TaskbarChangeIconToPlay();
                    stop_button.IsEnabled = false;
                    break;
            }
        }
        #endregion
    }
}
