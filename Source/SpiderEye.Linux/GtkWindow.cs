using System;
using Gtk;
using SpiderEye.Bridge;

namespace SpiderEye.Linux
{
    internal class GtkWindow : IWindow
    {
        public event CancelableEventHandler Closing;
        public event EventHandler Closed;
        public event EventHandler Shown;
        public event EventHandler Focused;

        public string Title
        {
            get => Window.GetTitle() ?? string.Empty;
            set => Window.SetTitle(value);
        }

        public Size Size
        {
            get
            {
                Window.GetDefaultSize(out int width, out int height);
                return new Size(width, height);
            }
            set
            {
                Window.SetDefaultSize((int)value.Width, (int)value.Height);
            }
        }

        public Size MinSize
        {
            get
            {
                Window.GetSizeRequest(out int width, out int height);
                return new Size(width, height);
            }
            set
            {
                Window.SetSizeRequest((int)value.Width, (int)value.Height);
            }
        }

        public Size MaxSize
        {
            get { return default; }
            set
            {
                // No-op, not supported
            }
        }

        public bool CanResize
        {
            get { return Window.GetResizable(); }
            set { Window.SetResizable(value); }
        }

        public string BackgroundColor
        {
            get { return backgroundColorField; }
            set
            {
                backgroundColorField = value;
                SetBackgroundColor(value);
                webview.UpdateBackgroundColor(value);
            }
        }

        public bool UseBrowserTitle
        {
            get { return webview.UseBrowserTitle; }
            set { webview.UseBrowserTitle = value; }
        }

        public AppIcon Icon
        {
            get { return iconField; }
            set
            {
                iconField = value;
                SetIcon(value);
            }
        }

        public bool EnableScriptInterface
        {
            get { return webview.EnableScriptInterface; }
            set { webview.EnableScriptInterface = value; }
        }

        public bool EnableDevTools
        {
            get { return webview.EnableDevTools; }
            set { webview.EnableDevTools = value; }
        }

        public Menu Menu
        {
            get { return menu; }
            set
            {
                menu = value;
                RefreshMenu();
            }
        }

        public IWebview Webview
        {
            get { return webview; }
        }

        object IWindow.NativeOptions => this;

        public Gtk.Window Window { get; }

        private readonly GtkWebview webview;
        private readonly WebviewBridge bridge;

        private readonly PopoverMenuBar menuBar;
        private bool shown;
        private bool disposed;
        private string backgroundColorField;
        private AppIcon iconField;
        private Menu menu;
        private string autosaveName = null;
        private CssProvider? cssProvider;

        public GtkWindow(WebviewBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            try
            {
                webview = new GtkWebview(bridge);
            }
            catch (DllNotFoundException)
            {
                var dialog = new GtkMessageBox
                {
                    Title = "Missing dependency",
                    Message = "The dependency 'libwebkitgtk-6.0-4' is missing. Make sure it is installed correctly.",
                    Buttons = MessageBoxButtons.Ok,
                };
                dialog.Show();
                Environment.Exit(-1);
            }

            Window = Gtk.Window.New();
            LinuxApplication.App.AddWindow(Window);

            var contentBox = Box.New(Orientation.Vertical, 0);
            Window.SetChild(contentBox);

            // Do not show the menu bar, since it could be empty
            menuBar = PopoverMenuBar.NewFromModel(Gio.Menu.New());
            contentBox.Append(menuBar);
            menuBar.Hide();

            webview.WebView.SetVexpand(true);
            contentBox.Append(webview.WebView);

            Window.OnShow += ShowCallback;
            Window.OnCloseRequest += DeleteCallback;
            Window.OnDestroy += DestroyCallback;
            Window.OnActivateFocus += FocusInCallback;

            webview.CloseRequested += Webview_CloseRequested;
            webview.TitleChanged += Webview_TitleChanged;
        }

        public void Show()
        {
            Window.Show();
            shown = true;
        }

        public void ShowModal(IWindow modalWindow)
        {
            if (modalWindow is not GtkWindow modalWinToShow)
            {
                return;
            }

            modalWinToShow.Window.SetTransientFor(Window);
            modalWinToShow.Window.DestroyWithParent = true;
            modalWinToShow.Window.SetModal(true);
            modalWinToShow.Show();
        }

        public void Close()
        {
            Window.Close();
        }

        public void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    Window.Unmaximize();
                    Window.Unminimize();
                    break;

                case WindowState.Maximized:
                    Window.Maximize();
                    break;

                case WindowState.Minimized:
                    Window.Minimize();
                    break;

                default:
                    throw new ArgumentException($"Invalid window state of \"{state}\"", nameof(state));
            }
        }

        public void RestoreAndAutoSavePosition(string name, Size defaultSize)
        {
            if (name == autosaveName)
            {
                // The position for the name is already set
                return;
            }

            if (autosaveName != null)
            {
                // Save information from previous name
                SaveWindowInformation();
            }

            autosaveName = name;

            if (Application.WindowInfoStorage == null)
            {
                throw new InvalidOperationException("Cannot auto save position without a window information storage.");
            }

            var savedInfo = Application.WindowInfoStorage.LoadWindowInformation(name);
            if (savedInfo == null)
            {
                ((IWindow)this).Size = defaultSize;
                return;
            }

            // It's not easy to change the position at this point
            // Checking the bounds is also not easy, so we currently skip this part
            Size = new Size(savedInfo.Bounds.Width, savedInfo.Bounds.Height);
            if (savedInfo.Maximised)
            {
                SetWindowState(WindowState.Maximized);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                webview.Dispose();
                Window.Destroy();
                Window.Dispose();
            }
        }

        private void SetIcon(AppIcon icon)
        {
            // TODO Currently a no-op.
            // Setting the icon dynamically isn't easy, as it relies on a IconTheme which reads files from a physical path
        }

        private void ShowCallback(Widget widget, EventArgs args)
        {
            Shown?.Invoke(this, EventArgs.Empty);
        }

        private bool DeleteCallback(Gtk.Window w, EventArgs a)
        {
            SaveWindowInformation();

            var args = new CancelableEventArgs();
            Closing?.Invoke(this, args);

            return args.Cancel;
        }

        private void DestroyCallback(Widget widget, EventArgs args)
        {
            webview.TitleChanged -= Webview_TitleChanged;
            bridge.TitleChanged -= Webview_TitleChanged;

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void FocusInCallback(Gtk.Window w, EventArgs args)
        {
            Focused?.Invoke(this, EventArgs.Empty);
        }

        private void Webview_TitleChanged(object sender, string title)
        {
            if (UseBrowserTitle)
            {
                Application.Invoke(() => Title = title ?? string.Empty);
            }
        }

        private void Webview_CloseRequested(object sender, EventArgs e)
        {
            Close();
        }

        private void SetBackgroundColor(string color)
        {
            if (cssProvider == null)
            {
                cssProvider = CssProvider.New();
                Window.GetStyleContext().AddProvider(cssProvider, 600);
            }

            cssProvider.LoadFromData($"* {{background-color: {color};}}", -1);
        }

        private void RefreshMenu()
        {
            if (menu?.NativeMenu is not GtkTopMenu nativeMenu)
            {
                menuBar.SetMenuModel(null);
                menuBar.Hide();
                return;
            }

            var existing = menuBar.GetMenuModel();
            menuBar.SetMenuModel(nativeMenu.BuildMenu());
            Window.InsertActionGroup(GtkTopMenu.MenuActionPrefix, null);
            Window.InsertActionGroup(GtkTopMenu.MenuActionPrefix, nativeMenu.BuildActionGroup());
            menuBar.Show();
            existing?.Dispose();
        }

        private void SaveWindowInformation()
        {
            if (autosaveName == null || Application.WindowInfoStorage == null)
            {
                return;
            }

            var size = Size;
            var windowInformation = new WindowInformation
            {
                Maximised = Window.IsMaximized(),
                Bounds = new Rectangle
                {
                    Height = (int)size.Height,
                    Width = (int)size.Width,
                },
            };
            Application.WindowInfoStorage.StoreWindowInformation(autosaveName, windowInformation);
        }
    }
}
