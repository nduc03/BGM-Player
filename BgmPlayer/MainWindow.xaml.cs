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
        private readonly AppState? initState;
        private AddOstWindow? addOstWindow = null;
        private System.Timers.Timer? fadeTimeout;

        private bool allowControlBySMTC = false;
        private int currentVolume = 100;
        private bool addOstWindowClosed = true;

        public MainWindow()
        {
            dispatcherTimer = new DispatcherTimer();
            initState = AppStateManager.LoadState();

            InitializeComponent();
            AudioPathManager.Init(initState ?? new AppState());
            AudioPathManager.InitTextBlock(IntroField, LoopField);
            SMTCManager.InitSMTC(OnPlayPause);
            SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
            SMTCManager.UpdateThumbnail();
            InitVolume();
            InitCheckbox();
            InitTimer();
#if ME
            menu_bar.Visibility = Visibility.Visible;
            InitBackgroundImage();
            InitTitleOption();
#endif

            UpdateAudioControlButton(AudioPlayer.CurrentState);
            AllowChooseFile(AudioPlayer.IsStopped);
            Title = SMTCManager.WindowTitle ?? SMTCManager.MusicTitle ?? AppConstants.DEFAULT_MUSIC_TITLE;
            stopfade_button.Content = stopfade_button.Content?.ToString()?.Replace("%d", AppConstants.STOP_FADE_DURATION.ToString());

            AudioPlayer.StateChanged += UpdateAudioControlButton;
        }

        #region Initialize
        private void InitVolume()
        {
            if (initState == null)
                Debug.WriteLine("InitVolume: config data is null, set volume to default value.");
            else if (initState.Volume == null)
                Debug.WriteLine("InitVolume: configData doesn't have Volume value");
            else if (initState.Volume >= 0 && initState.Volume <= AppConstants.VOLUME_SCALE)
                currentVolume = (int)initState.Volume;
            else
                currentVolume = (int)AppConstants.VOLUME_SCALE;

            AudioPlayer.SetVolume(currentVolume / AppConstants.VOLUME_SCALE);
            VolSlider.Value = currentVolume;
        }

        private void InitCheckbox()
        {
            if (initState != null && initState.AutoFill != null)
                autoFill.IsChecked = initState.AutoFill;
            else
            {
                autoFill.IsChecked = false;
                AppStateManager.SaveState(AutoFill: false);
            }
            AudioPathManager.AutoFill = autoFill.IsChecked ?? false;
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

#if ME
        private void InitBackgroundImage()
        {
            System.Windows.Media.ImageBrush background = new(new BitmapImage(new Uri("pack://application:,,,/img/schwarz_blured.png")))
            {
                Stretch = System.Windows.Media.Stretch.UniformToFill
            };
            Background = background;
        }

        /// <summary>
        /// Option 1: show all
        /// Option 2: Official title only
        /// Option 3: Translated title only (if applicable, else show official title)
        /// </summary>
        private void InitTitleOption()
        {
            TitleOption.Visibility = Visibility.Visible;
            switch (initState?.TitleOption)
            {
                case 2:
                    titleOfficialOnly.IsChecked = true;
                    break;
                case 3:
                    titleTransOnly.IsChecked = true;
                    break;
                default:
                    titleShowAll.IsChecked = true;
                    break;
            }
        }
#endif
        #endregion

        #region Button handler
        private void Reload_Click(object sender, RoutedEventArgs e)
        {
#if ME
            if (AudioPlayer.IsPlaying && 
                MessageBox.Show(
                    "Reload will stop the playing music.\nAre you sure?",
                    "Confirmation",
                    MessageBoxButton.YesNo
                ) == MessageBoxResult.No) return;
            Stop_Click(null, null);
            ExtendedOstInfoManager.ReloadContent();
            MessageBox.Show("Reloaded!");
#else
            MessageBox.Show("Version not supported.");
#endif
        }

        private void AddOst_Click(object sender, RoutedEventArgs e)
        {
            if (addOstWindow == null || addOstWindowClosed)
            {
                addOstWindow = new AddOstWindow();
                addOstWindow.Closed += (sender, e) =>
                {
                    addOstWindowClosed = true;
                };
                addOstWindow.Show();
                addOstWindowClosed = false;
            }
            else
            {
                if (addOstWindow.WindowState == WindowState.Minimized) addOstWindow.WindowState = WindowState.Normal;
                addOstWindow.Focus();
            }
        }
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
            switch (AudioPlayer.CurrentState)
            {
                case AudioState.STOP:
                    AllowChooseFile(false);
                    UpdateAudioControlButton(AudioState.PLAY);
                    allowControlBySMTC = true;
                    TaskbarChangeIconToPause();
                    SetVolume(currentVolume);

                    if (AudioPlayer.PlayBGM(AudioPathManager.Intro, AudioPathManager.Loop) == AudioPlayerState.FAILED)
                    {
                        MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        Stop_Click(null, null);
                        return;
                    }
                    timer.Start(); // Moved this after AudioPlayer.PlayBGM call, because sometimes opening stream from disk takes too much time which makes timer inaccurate
                    break;
                case AudioState.PAUSE:
                    if (AudioPlayer.Continue() == AudioPlayerState.OK)
                    {
                        UpdateAudioControlButton(AudioState.PLAY);
                        timer.Start();
                    }
                    else
                    {
                        MessageBox.Show("Unknown error!", "Error!", MessageBoxButton.OK, MessageBoxImage.Error);
                        Stop_Click(null, null);
                    }
                    break;
                case AudioState.PLAY:
                    AudioPlayer.Pause();
                    UpdateAudioControlButton(AudioState.PAUSE);
                    TaskbarChangeIconToPlay();
                    timer.Stop();
                    break;
            }
        }

        private void Stop_Click(object? sender, RoutedEventArgs? e)
        {
            AudioPlayer.Stop();
            play_pause_button.IsEnabled = true;
            AllowChooseFile(true);
            UpdateAudioControlButton(AudioState.STOP);
            TaskbarChangeIconToPlay();
            timer.Stop();
            timer.Reset();
        }

        private void StopFade_Click(object? sender, RoutedEventArgs? e)
        {
            AudioPlayer.StopFade(AppConstants.STOP_FADE_DURATION);
            play_pause_button.IsEnabled = false;
            stop_button.IsEnabled = false;
            fadeTimeout = new()
            {
                Interval = AppConstants.STOP_FADE_DURATION * 1000,
                AutoReset = false
            };
            fadeTimeout.Elapsed += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    Stop_Click(sender, e);
                });
                fadeTimeout.Stop();
                fadeTimeout.Dispose();
            };
            fadeTimeout.Start();
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
        private void UpdateTitleOption(object sender, RoutedEventArgs e)
        {
#if ME
            System.Windows.Controls.RadioButton? opttion = sender as System.Windows.Controls.RadioButton;
            switch (opttion?.Name) 
            {
                case "titleOfficialOnly":
                    AppStateManager.SaveState(TitleOption: 2);
                    break;
                case "titleTransOnly":
                    AppStateManager.SaveState(TitleOption: 3);
                    break;
                default:
                    AppStateManager.SaveState(TitleOption: 1);
                    break;
            }
            SMTCManager.UpdateTitle(AudioPathManager.Intro, AudioPathManager.Loop);
#endif
        }
#endregion

        #region Taskbar handler
        private void TaskbarPlayPause_handler(object? sender, EventArgs? e)
        {
            if ((AudioPlayer.IsPaused || AudioPlayer.IsStopped) && SMTCManager.IsEnable)
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
                StopFade_Click(sender, null);
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
            AppStateManager.SaveState(AutoFill: true);
            AudioPathManager.AutoFill = true;
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            AppStateManager.SaveState(AutoFill: false);
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
            AppStateManager.SaveState(Volume: Volume);
        }
        private void UpdateAudioControlButton(AudioState audioState)
        {
            switch (audioState)
            {
                case AudioState.PLAY:
                    play_pause_button.Content = "Pause";
                    TaskbarChangeIconToPause();
                    stop_button.IsEnabled = true;
                    stopfade_button.IsEnabled = true;
                    break;
                case AudioState.PAUSE:
                    play_pause_button.Content = "Play";
                    TaskbarChangeIconToPlay();
                    stop_button.IsEnabled = true;
                    stopfade_button.IsEnabled = true;
                    break;
                case AudioState.STOP:
                    play_pause_button.Content = "Play";
                    TaskbarChangeIconToPlay();
                    stop_button.IsEnabled = false;
                    stopfade_button.IsEnabled = false;
                    break;
            }
        }
        #endregion
    }
}
