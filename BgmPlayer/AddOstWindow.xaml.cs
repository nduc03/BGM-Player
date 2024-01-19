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
            if (InGameName == string.Empty || Title == string.Empty)
            {
                MessageBox.Show("Required parts are not filled!");
                return;
            }
            var TranlatedTitle = translated_title.Text.Trim() == string.Empty ? null : translated_title.Text.Trim();
            var Artist = artist.Text.Trim() == string.Empty ? null : artist.Text.Trim();
            var EventName = event_name.Text.Trim() == string.Empty ? null : event_name.Text.Trim();
            OstList.AddOrReplaceContent(InGameName, Title, TranlatedTitle, Artist, EventName);
            MessageBox.Show("Done!");
            Close();
#endif
        }
    }
}
