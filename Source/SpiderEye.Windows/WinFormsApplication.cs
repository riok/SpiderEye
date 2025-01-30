using System;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using App = System.Windows.Forms.Application;

namespace SpiderEye.Windows
{
    internal class WinFormsApplication : IApplication
    {
        public IUiFactory Factory { get; }

        public SynchronizationContext SynchronizationContext { get; }

        // Disable the experimental warning. Should no longer be experimental with .NET 10
#pragma warning disable WFO5001
        public bool? IsDarkModeEnabled => App.IsDarkModeEnabled;
#pragma warning restore WFO5001

        public WinFormsApplication()
        {
            App.EnableVisualStyles();
            App.SetCompatibleTextRenderingDefault(false);
            App.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            Factory = new WinFormsUiFactory();
            SynchronizationContext = new WindowsFormsSynchronizationContext();

            Application.OpenWindows.AllWindowsClosed += Application_AllWindowsClosed;
        }

        public void Run()
        {
            App.Run();
        }

        public void Exit()
        {
            App.Exit();
        }

        public void ApplyTheme(ApplicationTheme theme)
        {
            // Disable the experimental warning. Should no longer be experimental with .NET 10
#pragma warning disable WFO5001
            var colorMode = theme switch
            {
                ApplicationTheme.OsDefault => SystemColorMode.System,
                ApplicationTheme.Light => SystemColorMode.Classic,
                ApplicationTheme.Dark => SystemColorMode.Dark,
                _ => SystemColorMode.System,
            };

            var wasDarkMode = IsDarkModeEnabled;
            App.SetColorMode(colorMode);
#pragma warning restore WFO5001

            if (wasDarkMode == IsDarkModeEnabled)
            {
                // The effective color mode has not changed
                return;
            }

            foreach (Form form in App.OpenForms)
            {
                if (!form.Visible)
                {
                    continue;
                }

                // Workaround to apply the theme to the already opened (shown) forms
                // Without this, the Windows title bar would keep the old color
                form.Hide();
                form.Show();
            }
        }

        private void Application_AllWindowsClosed(object sender, EventArgs e)
        {
            if (Application.ExitWithLastWindow) { Exit(); }
        }
    }
}
