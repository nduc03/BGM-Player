using System;
using System.Threading;
using System.Windows;

namespace bgmPlayer
{
    public partial class App : Application
    {
        public static System.Windows.Forms.NotifyIcon icon;
        private static Mutex? mutex = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new Mutex(true, "BgmPlayer", out bool IsNewInstance);
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
            menuItem.Click += new EventHandler(LeftClick);
            menuItem.Name = "Exit";
            icon = new System.Windows.Forms.NotifyIcon();
            icon.DoubleClick += new EventHandler(ShowMainWindow);
            icon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            icon.Icon = new System.Drawing.Icon(GetResourceStream(new Uri(IconUri, UriKind.Relative)).Stream);
            icon.Visible = true;
            icon.ContextMenuStrip.Items.Add(menuItem);
        }

        private void LeftClick(object? sender, EventArgs? e)
        {
            Current.Shutdown();
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
