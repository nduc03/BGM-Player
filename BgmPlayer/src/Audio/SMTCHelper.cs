using System;
using System.IO;
using System.Windows;
using Windows.Foundation;
using Windows.Media;
using Windows.Storage.Streams;

namespace bgmPlayer
{
    public static class SMTCHelper
    {
        public static bool IsSmtcEnable
        {
            get
            {
                if (!isInitialized) return false;
                return smtc.IsEnabled;
            }
        }

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
            PathHelper.AllEmpty += Disable;
        }

        public static void Enable() 
        {
            smtc.IsEnabled = true;
        }

        public static void Disable()
        {
            smtc.IsEnabled = false;
        }

        public static void UpdateStatus(MediaPlaybackStatus playbackStatus)
        {
            smtc.PlaybackStatus = playbackStatus;
        }

        public static void UpdateTitle(string? IntroPath, string? LoopPath)
        {
            if (!isInitialized) return;
            string? title = IntroPath == null || LoopPath == null ? 
                AppConstants.DEFAULT_MUSIC_TITLE : Utils.GetBgmFileName(IntroPath, LoopPath);
            if (title == null)
            {
                if (IntroPath != string.Empty && LoopPath == string.Empty)
                    title = Path.GetFileNameWithoutExtension(IntroPath);
                else if (LoopPath != string.Empty && IntroPath == string.Empty)
                    title = Path.GetFileNameWithoutExtension(LoopPath);
                else title = AppConstants.DEFAULT_MUSIC_TITLE;
            }
            else
            {
#if ME
                if (!File.Exists(AppConstants.DISABLE_OST_NAME))
                    title = Utils.GetArknightsOstName(title);
#endif
            }
            updater.MusicProperties.Title = title ?? AppConstants.DEFAULT_MUSIC_TITLE;
            Application.Current.MainWindow.Title = title ?? AppConstants.DEFAULT_MUSIC_TITLE;
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
