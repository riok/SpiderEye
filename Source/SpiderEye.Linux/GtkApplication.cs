using System;
using System.Threading;
using Gio;

namespace SpiderEye.Linux
{
    internal class GtkApplication : IApplication
    {
        public IUiFactory Factory { get; }

        public SynchronizationContext SynchronizationContext { get; } // TODO remove?

        private bool hasExited = false;
        private Gtk.Application application;

        public GtkApplication()
        {
            Init();

            Factory = new GtkUiFactory();
            SynchronizationContext = new GtkSynchronizationContext();

            Application.OpenWindows.AllWindowsClosed += Application_AllWindowsClosed;
        }

        public bool? IsDarkModeEnabled => null;

        public void Run()
        {
            application.Run(0, null);
        }

        public void Exit()
        {
            if (!hasExited)
            {
                hasExited = true;
                application.Quit();
                application.Dispose();
            }
        }

        public void ApplyTheme(ApplicationTheme theme)
        {
            // No-op on linux.
            // TODO check gtk-application-prefer-dark-theme
            // TODO maybe gtk_widget_queue_draw is needed afterwards

            /*
    var buf = GtkSource.Buffer.New(null);
    var view = GtkSource.View.NewWithBuffer(buf);
            if (settings?.GtkApplicationPreferDarkTheme == true ||
                settings?.GtkThemeName?.ToLower()?.Contains("dark") == true)
                buf.SetStyleScheme(GtkSource.StyleSchemeManager.GetDefault().GetScheme("Adwaita-dark"));
                */
        }

        private void Init()
        {
            WebKit.Module.Initialize();
            // TODO supply application id
            application = Gtk.Application.New("test.app.SpiderEye", ApplicationFlags.FlagsNone);
        }

        private void Application_AllWindowsClosed(object sender, EventArgs e)
        {
            if (Application.ExitWithLastWindow) { Exit(); }
        }
    }
}
