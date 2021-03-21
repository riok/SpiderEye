using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace SpiderEye.Playground.Core
{
    public abstract class ProgramBase
    {
        private static Window _mainWindow;
        private static ServiceProvider _serviceProvider;

        protected static void Run()
        {
            // var icon = AppIcon.FromFile("icon", ".");

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<UiBridge>();
            Application.AddGlobalHandler<UiBridge>();
            using var serviceProvider = serviceCollection.BuildServiceProvider();
            _serviceProvider = serviceProvider;

            using (var window = new Window(serviceProvider))
            {
                _mainWindow = window;
                window.Title = "SpiderEye Playground";
                window.UseBrowserTitle = true;
                window.EnableScriptInterface = true;
                window.CanResize = true;
                window.BackgroundColor = "#303030";
                window.Size = new Size(800, 600);
                window.MinSize = new Size(300, 200);
                window.MaxSize = new Size(1200, 900);
                // window.Icon = icon;
                window.Navigating += (sender, uri) => Console.WriteLine("uri changed: " + uri);

                var windowMenu = new Menu();
                var appMenu = windowMenu.MenuItems.AddLabelItem("App");
                appMenu.MenuItems.AddMacOsHide();
                appMenu.MenuItems.AddMacOsHideOtherApplications();
                appMenu.MenuItems.AddMacOsUnhideAllApplications();
                appMenu.MenuItems.AddMacOsSeparator();

                var quitMenu = appMenu.MenuItems.AddLabelItem("Quit");
                quitMenu.SetSystemShortcut(SystemShortcut.Close);
                quitMenu.Click += (s, e) => Application.Exit();

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

                SetDevSettings(window);

                // the port number is defined in the angular.json file (under "architect"->"serve"->"options"->"port")
                // note that you have to run the angular dev server first (npm run watch)
                Application.UriWatcher = new AngularDevUriWatcher("http://localhost:65400");
                Application.ContentProvider = new EmbeddedContentProvider("Angular/dist");
                // Application.Run(window, "/index.html");
                Application.Run(window, "https://nd875.csb.app/");
            }
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
