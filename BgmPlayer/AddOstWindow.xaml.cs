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
            var Title = title.Text.Trim();
            var TranlatedTitle = title.Text == string.Empty ? null : title.Text.Trim();
            var Artist = artist.Text == string.Empty ? null : title.Text.Trim();
            var EventName = event_name.Text == string.Empty ? null : event_name.Text.Trim();
            OstList.AddOrReplaceContent(InGameName, Title, TranlatedTitle, Artist, EventName);
            MessageBox.Show("Done!");
            Close();
#endif
        }
    }
}
