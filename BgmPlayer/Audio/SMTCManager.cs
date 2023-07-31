using System;
using System.IO;
using System.Windows;
using Windows.Foundation;
using Windows.Media;
using Windows.Storage.Streams;

namespace bgmPlayer
{
    public static class SMTCManager
    {
        public static bool IsEnable
        {
            get
            {
                if (!isInitialized) return false;
                return smtc.IsEnabled;
            }
            set { smtc.IsEnabled = value; }
        }
        public static string? Title { get; private set; }
        public static string? WindowTitle { get; private set; }

        private static readonly Windows.Media.Playback.MediaPlayer mediaPlayer = new();
        private static readonly SystemMediaTransportControls smtc = mediaPlayer.SystemMediaTransportControls;
        private static readonly SystemMediaTransportControlsDisplayUpdater updater = smtc.DisplayUpdater;

        private static object? keepThumbnailOpen = null;
        private static bool isInitialized = false;

        public static void InitSMTC(
            TypedEventHandler<SystemMediaTransportControls, SystemMediaTransportControlsButtonPressedEventArgs> OnPlayPause
            )
        {
            mediaPlayer.CommandManager.IsEnabled = false;
            smtc.IsPlayEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.IsStopEnabled = true;
            smtc.IsNextEnabled = false;
            smtc.IsPreviousEnabled = false;
            smtc.ButtonPressed += OnPlayPause;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = AppConstants.DEFAULT_MUSIC_TITLE;
            UpdateThumbnail();
            smtc.IsEnabled = true;
            isInitialized = true;
            AudioPathManager.AllEmpty += () =>
            {
                IsEnable = false;
            };
        }

        public static void UpdateStatus(MediaPlaybackStatus playbackStatus)
        {
            smtc.PlaybackStatus = playbackStatus;
        }
        public static void UpdateTitle(string? IntroPath, string? LoopPath)
        {
            if (!isInitialized) return;
            Title = Utils.GetBgmFileName(IntroPath, LoopPath, AppConstants.DEFAULT_MUSIC_TITLE);
#if ME
            int? titleOption = PersistedStateManager.LoadState()?.TitleOption;
            if (!File.Exists(AppConstants.DISABLE_OST_NAME))
            {
                OstInfo? info = Utils.GetArknightsOstInfo(Title);
                if (info == null)
                {
                    updater.MusicProperties.Artist = string.Empty;
                    Title ??= AppConstants.DEFAULT_MUSIC_TITLE;
                    WindowTitle = null;
                }
                else
                {
                    Title = info.Value.GetParsedTitle(titleOption);
                    WindowTitle = info.Value.GetWindowTitle(titleOption);
                    updater.MusicProperties.Artist = info.Value.Artist ?? string.Empty;
                }
            }
#endif
            updater.MusicProperties.Title = Title;
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.Title = WindowTitle ?? Title;
            updater.Update();
        }

        public static async void UpdateThumbnail()
        {
            if (!isInitialized) return;
            // Create temp file as a workaround since creating thumbnail
            // from RandomAccessStreamReference.CreateFromStream doesn't work
            if (!Utils.IsValidCache())
            {
                Directory.CreateDirectory(AppConstants.CACHE_FOLDER).Attributes = FileAttributes.Hidden;
                using var file = File.Create(AppConstants.THUMBNAIL_CACHE_LOCATION);
                var imgUri = "img/sound.jpg";
#if ME
                imgUri = "img/schwarz.jpg";
#endif
                var stream = Application.GetResourceStream(new Uri(imgUri, UriKind.Relative)).Stream;
                stream.CopyTo(file);
            }
            keepThumbnailOpen ??= File.Open(AppConstants.THUMBNAIL_CACHE_LOCATION, FileMode.Open, FileAccess.Read, FileShare.Read);
            updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(
                await Windows.Storage.StorageFile.GetFileFromPathAsync(
                        AppDomain.CurrentDomain.BaseDirectory + AppConstants.THUMBNAIL_CACHE_LOCATION.Replace("/", "\\")
                    )
            );
            updater.Update();
        }
    }
}
