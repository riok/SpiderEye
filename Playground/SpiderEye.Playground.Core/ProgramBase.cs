﻿using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using SpiderEye.Playground.Core.Bridge;

namespace SpiderEye.Playground.Core
{
    public abstract class ProgramBase
    {
        private const string LightBackgroundColor = "#ffffff";
        private const string DarkBackgroundColor = "#1e1e1e";
        public static Uri CustomFileHost { get; private set; }

        private static Window _mainWindow;
        private static ServiceProvider _serviceProvider;

        protected static void Run()
        {
            var icon = AppIcon.FromFile("icon", ".");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSpidereyeBridgeClientService<IUiBridgeClientService>();
            serviceCollection.AddScoped<UiBridge>();
            Application.AddGlobalHandler<UiBridge>();
            Application.WindowInfoStorage = new WindowInformationStorage();

            if (Application.LinuxOptions is { } linuxOptions)
            {
                linuxOptions.ApplicationId = "test.spidereye.Playground";
                linuxOptions.ApplicationFlags = LinuxApplicationFlags.NonUnique;
            }

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceProvider = serviceProvider;

            using var window = new Window(serviceProvider);
            _mainWindow = window;
            window.Title = "SpiderEye Playground";
            window.UseBrowserTitle = true;
            window.EnableScriptInterface = true;
            window.CanResize = true;
            window.BackgroundColor = LightBackgroundColor;
            window.MinSize = new Size(300, 200);
            window.Icon = icon;
            window.Navigating += (_, uri) => Console.WriteLine("uri changed: " + uri.Url);
            window.RestoreAndAutoSavePosition("main", new Size(800, 600));

            var windowMenu = new Menu();
            var appMenu = windowMenu.MenuItems.AddLabelItem("App");
            appMenu.MenuItems.AddMacOsHide();
            appMenu.MenuItems.AddMacOsHideOtherApplications();
            appMenu.MenuItems.AddMacOsUnhideAllApplications();
            appMenu.MenuItems.AddMacOsSeparator();

            var quitMenu = appMenu.MenuItems.AddLabelItem("Quit");
            quitMenu.SetSystemShortcut(SystemShortcut.Close);
            quitMenu.Click += (s, e) => Application.Exit();

            var darkModeMenu = appMenu.MenuItems.AddLabelItem("Enable dark mode");
            darkModeMenu.Click += (_, _) =>
            {
                Application.ApplyTheme(ApplicationTheme.Dark);
                foreach (var w in Application.OpenWindows)
                {
                    w.BackgroundColor = DarkBackgroundColor;
                }
            };

            var lightModeMenu = appMenu.MenuItems.AddLabelItem("Enable light mode");
            lightModeMenu.Click += (_, _) =>
            {
                Application.ApplyTheme(ApplicationTheme.Light);
                foreach (var w in Application.OpenWindows)
                {
                    w.BackgroundColor = LightBackgroundColor;
                }
            };

            var osDefaultThemeMenu = appMenu.MenuItems.AddLabelItem("Enable OS default theme");
            osDefaultThemeMenu.Click += (_, _) =>
            {
                Application.ApplyTheme(ApplicationTheme.OsDefault);
            };

            windowMenu.MenuItems.AddMacOsEdit();
            windowMenu.MenuItems.AddMacOsView();

            var mainMenu = windowMenu.MenuItems.AddLabelItem("Main Menu");
            mainMenu.MenuItems.AddLabelItem("Entry 1");
            mainMenu.MenuItems.AddSeparatorItem();
            mainMenu.MenuItems.AddLabelItem("Entry 2");
            var showModalMenu = mainMenu.MenuItems.AddLabelItem("Show Modal");
            showModalMenu.Click += (s, e) => ShowModalMenu_Click(true);
            showModalMenu.SetShortcut(ModifierKey.Control | ModifierKey.Shift, Key.M);

            var showWindowMenu = mainMenu.MenuItems.AddLabelItem("Show Window");
            showWindowMenu.Click += (s, e) => ShowModalMenu_Click(false);
            showWindowMenu.SetShortcut(ModifierKey.Control | ModifierKey.Shift, Key.W);

            windowMenu.MenuItems.AddMacOsWindow();

            var helpMenu = windowMenu.MenuItems.AddLabelItem("Help");
            var helpItem = helpMenu.MenuItems.AddLabelItem("MyHelp");
            helpItem.SetSystemShortcut(SystemShortcut.Help);

            window.Menu = windowMenu;

            if (window.MacOsOptions != null)
            {
                window.MacOsOptions.Appearance = MacOsAppearance.DarkAqua;
            }

            CustomFileHost = window.RegisterLocalDirectoryMapping(Path.GetFullPath("."));

            SetDevSettings(window);

            Application.InternalError += (_, args) =>
            {
                Console.WriteLine("Internal Error: " + args.Message);
                Console.WriteLine(args.Exception);
            };

            // the port number is defined in the angular.json file (under "architect"->"serve"->"options"->"port")
            // note that you have to run the angular dev server first (npm run watch)
            Application.UriWatcher = new AngularDevUriWatcher("http://localhost:65400");
            Application.ContentProvider = new EmbeddedContentProvider("Angular/dist");
            Application.Run(window, "/index.html");
        }

        private static void ShowModalMenu_Click(bool modal)
        {
            var window = new Window(_serviceProvider) { Title = "this is a modal" };
            window.Closed += DisposeWindow;
            window.UseBrowserTitle = true;
            window.LoadUrl("https://www.google.com/search?q=foo%20bar");

            window.Navigating += (x, args) =>
            {
                if (args.Url.Host.EndsWith("wikipedia.org", StringComparison.Ordinal))
                {
                    args.Cancel = true;
                    window.Close();
                }
            };

            if (modal)
            {
                _mainWindow.ShowModal(window);
            }
            else
            {
                window.Show();
            }
        }

        private static void DisposeWindow(object sender, EventArgs e)
        {
            if (!(sender is Window d))
            {
                return;
            }

            d.Closed -= DisposeWindow;
            d.Dispose();
        }

        private static void ShowItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                $"Hello World from the SpiderEye Playground running on {Application.OS}",
                "Hello World",
                MessageBoxButtons.Ok);
        }

        [Conditional("DEBUG")]
        private static void SetDevSettings(Window window)
        {
            window.EnableDevTools = true;
        }
    }
}
