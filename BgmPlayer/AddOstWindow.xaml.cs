using System.Windows;

namespace bgmPlayer
{
    /// <summary>
    /// Interaction logic for AddOstWindow.xaml
    /// </summary>
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
            if (InGameName == string.Empty || OstTitle == string.Empty)
            {
                MessageBox.Show("Required parts are not filled!");
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
            var TranlatedTitle = translated_title.Text.Trim() == string.Empty ? null : translated_title.Text.Trim();
            var Artist = artist.Text.Trim() == string.Empty ? null : artist.Text.Trim();
            var EventName = event_name.Text.Trim() == string.Empty ? null : event_name.Text.Trim();
            OstList.AddOrReplaceContent(InGameName, OstTitle, TranlatedTitle, Artist, EventName);
            MessageBox.Show("Done!");
            Close();
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
                    """;
                MessageBox.Show(info);
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
