using System;
using System.Threading;
using System.Windows;

namespace bgmPlayer
{
    public partial class App : Application
    {
        private static System.Windows.Forms.NotifyIcon? icon;
        private static Mutex? mutex = null;
        private static bool exitBoxShowing = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            var MutexName = "BgmPlayer";
#if DEBUG
            MutexName = "BgmPlayerDebug";
#endif
            mutex = new Mutex(true, MutexName, out bool IsNewInstance);
            if (!IsNewInstance)
            {
                MessageBox.Show("App is running.");
                Current.Shutdown();
            }
            InitNotifyIcon();
            base.OnStartup(e);
        }

        private void InitNotifyIcon()
        {
            var IconUri = "icon/music.ico";
#if ME
            IconUri = "icon/dusk_arknights.ico";
#endif
            System.Windows.Forms.ToolStripMenuItem menuItem = new("Exit");
            menuItem.Click += new EventHandler(ExitApp);
            menuItem.Name = "Exit";
            icon = new System.Windows.Forms.NotifyIcon
            {
                Text = "BGM Player",
                Icon = new System.Drawing.Icon(GetResourceStream(new Uri(IconUri, UriKind.Relative)).Stream),
                Visible = true,
                ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip()
            };
            icon.DoubleClick += new EventHandler(ShowMainWindow);
            icon.ContextMenuStrip.Items.Add(menuItem);
        }

        private void ExitApp(object? sender, EventArgs? e)
        {
            if (exitBoxShowing) return;
            exitBoxShowing = true;
            if (MessageBox.Show("Are you sure to quit?", "Exit confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileHelper.InstantSaveState();
                Shutdown();
            }
            exitBoxShowing = false;
        }
        private void ShowMainWindow(object? sender, EventArgs? e)
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            else if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }
    }
}
