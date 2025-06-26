using System;
using System.Collections.Generic;
using System.Threading;
using GLib.Internal;

namespace SpiderEye.Linux
{
    internal class GtkApplication : IApplication, ILinuxApplicationOptions
    {
        public IUiFactory Factory { get; }

        public SynchronizationContext SynchronizationContext { get; }

        private bool hasExited;
        private bool activated;
        private List<Gtk.Window> windowsToActivate = new();

        object IApplication.NativeOptions => this;

        public GtkApplication()
        {
            Init();

            Factory = new GtkUiFactory();
            SynchronizationContext = new MainLoopSynchronizationContext();

            Application.OpenWindows.AllWindowsClosed += Application_AllWindowsClosed;
        }

        public bool? IsDarkModeEnabled => null;

        internal Gtk.Application NativeApplication { get; private set; }

        public string ApplicationId { get; set; }

        public LinuxApplicationFlags ApplicationFlags { get; set; }

        public void Run()
        {
            NativeApplication.OnActivate += (sender, args) =>
            {
                activated = true;

                foreach (var window in windowsToActivate)
                {
                    window.SetApplication(NativeApplication);
                }
            };

            if (!string.IsNullOrEmpty(ApplicationId))
            {
                NativeApplication.ApplicationId = ApplicationId;
            }

            NativeApplication.Run(0, null);
        }

        public void Exit()
        {
            if (!hasExited)
            {
                hasExited = true;
                NativeApplication.Quit();
                NativeApplication.Dispose();
            }
        }

        public void ApplyTheme(ApplicationTheme theme)
        {
            // No-op on linux.
            // TODO check gtk-application-prefer-dark-theme
        }

        internal void AddWindow(Gtk.Window window)
        {
            if (!activated)
            {
                windowsToActivate.Add(window);
            }
            else
            {
                window.SetApplication(NativeApplication);
            }
        }

        private void Init()
        {
            WebKit.Module.Initialize();
            NativeApplication = Gtk.Application.New(ApplicationId, (Gio.ApplicationFlags)ApplicationFlags);
        }

        private void Application_AllWindowsClosed(object sender, EventArgs e)
        {
            if (Application.ExitWithLastWindow) { Exit(); }
        }
    }
}
