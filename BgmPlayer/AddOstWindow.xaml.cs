using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace bgmPlayer
{
    public partial class AddOstWindow : Window
    {
        public AddOstWindow()
        {
            InitializeComponent();
#if !ME
            MessageBox.Show("Version not supported.");
            Close();
#endif
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
#if ME
            var InGameName = in_game_name.Text.Trim();
            var OstTitle = title.Text.Trim();
            var TranlatedTitle = translated_title.Text.Trim() == string.Empty ? null : translated_title.Text.Trim();
            var Artist = artist.Text.Trim() == string.Empty ? null : artist.Text.Trim();
            var EventName = event_name.Text.Trim() == string.Empty ? null : event_name.Text.Trim();
            var Transource = trans_source.Text.Trim() == string.Empty ? null : trans_source.Text.Trim();

            if (InGameName == string.Empty || OstTitle == string.Empty
                || (Transource != null && TranlatedTitle == null) // add translated source without adding translated title
                )
            {
                MessageBox.Show("Required parts are missing!");
                return;
            }
            var ost = Utils.GetArknightsOstInfo(InGameName);
            if (ost != null)
            {
                if (!ost.Value.IsDynamic)
                {
                    MessageBox.Show("This OST info cannot be changed.");
                    return;
                }
                var confirm = MessageBox.Show(
                        "This OST info is already existed. Do you want to replace?",
                        "Confirmation!",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                if (confirm == MessageBoxResult.No) return;
            }

            ExtendedOstInfoManager.AddOrEditContent(InGameName, OstTitle, TranlatedTitle, Artist, EventName, Transource);
            if (MessageBox.Show("Done! Close window?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Close();
#endif
        }

        private void Check_Click(object sender, RoutedEventArgs e)
        {
#if ME
            var inGameName = in_game_name.Text.Trim();
            if (inGameName == string.Empty) return;
            var ost = Utils.GetArknightsOstInfo(inGameName);
            if (ost == null)
            {
                MessageBox.Show($"No infomation available for \"{inGameName}\".");
            }
            else
            {
                var info = $"""
                    Official Title: {ost.Value.Title}
                    Translated Title: {ost.Value.TranslatedTitle ?? "No info"}
                    Artist: {ost.Value.Artist ?? "No info"}
                    Event Name: {ost.Value.EventName ?? "No info"}
                    Can Modify: {(ost.Value.IsDynamic ? "Yes" : "No")}
                    Translation Source: {((ost.Value.TranslatedTitle != null) ? (ExtendedOstInfoManager.Data?[inGameName]?["TranslationSource"] ?? "Unknown") : "Not applicable")}
                    """;
                MessageBox.Show(info);
            }
#endif
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
#if ME
            var inGameName = in_game_name.Text.Trim();
            if (inGameName == string.Empty) return;
            var ost = Utils.GetArknightsOstInfo(inGameName);
            if (ost == null) return;
            else
            {
                title.Text = ost.Value.Title;
                translated_title.Text = ost.Value.TranslatedTitle ?? string.Empty;
                artist.Text = ost.Value.Artist ?? string.Empty;
                event_name.Text = ost.Value.EventName ?? string.Empty;
                if (translated_title.Text != string.Empty)
                    trans_source.Text = ExtendedOstInfoManager.Data?[inGameName]?["TranslationSource"]?.ToString() ?? string.Empty;
            }
#endif
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
#if ME
            ProcessStartInfo process = new()
            {
                FileName = "code",
                Arguments = $"\"{ExtendedOstInfoManager.FilePath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            try
            {
                Process.Start(process);
            }
            catch (Win32Exception)
            {
                if (MessageBox.Show("Cannot open VS code, try open in notepad instead?", "Error", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    process.FileName = "notepad";
                    Process.Start(process);
                }
            }
#endif
        }

        private void Current_Click(object sender, RoutedEventArgs e)
        {
            var inGameName = Utils.GetBgmFileName(AudioPathManager.Intro, AudioPathManager.Loop);
            if (inGameName != null) in_game_name.Text = inGameName;
        }
    }
}
