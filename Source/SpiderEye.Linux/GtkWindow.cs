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

        private readonly IntPtr menuBarHandle;
        private readonly IntPtr accelGroup;
        private bool shown;
        private bool disposed;
        private string backgroundColorField;
        private AppIcon iconField;
        private Menu menu;
        private string autosaveName = null;

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
                    Message = "The dependency 'libwebkit2gtk-4.0' is missing. Make sure it is installed correctly.",
                    Buttons = MessageBoxButtons.Ok,
                };
                dialog.Show();
                Environment.Exit(-1);
            }

            Window = Gtk.Window.New();

            var contentBox = Box.New(Orientation.Vertical, 0);
            Window.SetChild(webview.WebView);
            contentBox.Show();

            // Do not show the menu bar, since it could be empty
            /*
             TODO handle menubar (probably globally like in macOS)
             menuBarHandle = Gtk.MenuBar.Create();
            Gtk.Box.AddChild(contentBox, menuBarHandle, false, false, 0);

            contentBox.Append(webview, true, true, 0);
            Gtk.Widget.Show(webview.Handle);*/

/*      TODO needed?
            accelGroup = Gtk.AccelGroup.Create();
            Gtk.Window.AddAccelGroup(Handle, accelGroup);*/

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

        private unsafe void SetIcon(AppIcon icon)
        {/* TODO maybe not that easy...
            if (icon == null || icon.Icons.Length == 0)
            {
                window.SetIconName(null);
            }
            else
            {
                Gtk.IconTheme.New().icon
                IntPtr iconList = IntPtr.Zero;
                var icons = new IntPtr[icon.Icons.Length];
                var icon = Gio.BytesIcon.New(GLib.Bytes.New(icon.GetIconData(icon.Icons[0])));
                var theme = Gtk.IconTheme.New();
                theme.
                try
                {
                    for (int i = 0; i < icons.Length; i++)
                    {
                        IntPtr iconStream = IntPtr.Zero;
                        try
                        {
                            byte[] data = icon.GetIconData(icon.Icons[i]);
                            fixed (byte* iconDataPtr = data)
                            {
                                iconStream = GLib.CreateStreamFromData((IntPtr)iconDataPtr, data.Length, IntPtr.Zero);
                                icons[i] = Gdk.Pixbuf.NewFromStream(iconStream, IntPtr.Zero, IntPtr.Zero);
                                iconList = GLib.ListPrepend(iconList, icons[i]);
                            }
                        }
                        finally { if (iconStream != IntPtr.Zero) { GLib.UnrefObject(iconStream); } }
                    }

                    Gtk.Window.SetIconList(Handle, iconList);
                }
                finally
                {
                    if (iconList != IntPtr.Zero) { GLib.FreeList(iconList); }
                    foreach (var item in icons)
                    {
                        if (item != IntPtr.Zero) { GLib.UnrefObject(item); }
                    }
                }
            }*/
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
            using var cssProvider = CssProvider.New();
            cssProvider.LoadFromString($"* {{background-color:{color}}}");
            Window.GetStyleContext().AddProvider(cssProvider, 600);
        }

        private void RefreshMenu()
        {
            ClearMenu();
            PopulateMenu();
/* TODO menu bar similar to macOS
            if (menu.MenuItems.Count > 0)
            {
                Gtk.Widget.ShowAll(menuBarHandle);
            }
            else
            {
                Gtk.Widget.Hide(menuBarHandle);
            }*/
        }

        private void PopulateMenu()
        {
            if (menu == null)
            {
                return;
            }
/* TODO
            var nativeMenu = NativeCast.To<GtkMenu>(menu.NativeMenu);
            nativeMenu.SetAccelGroup(accelGroup);

            foreach (var menuItem in nativeMenu.GetItems())
            {
                Gtk.Widget.ContainerAdd(menuBarHandle, menuItem.Handle);
            }*/
        }

        private void ClearMenu()
        {/* TODO
            IntPtr existingMenuList = Gtk.Widget.GetChildren(menuBarHandle);
            for (uint i = 0; i < GLib.GetListLength(existingMenuList); i++)
            {
                var existingMenu = GLib.GetListNthData(existingMenuList, i);
                Gtk.Widget.Destroy(existingMenu);
            }

            GLib.FreeList(existingMenuList);*/
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
