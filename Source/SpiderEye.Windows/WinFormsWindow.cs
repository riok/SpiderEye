using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpiderEye.Bridge;
using SpiderEye.Tools;
using SDSize = System.Drawing.Size;

namespace SpiderEye.Windows
{
    internal class WinFormsWindow : Form, IWindow
    {
        private const float LogicalDpi = 96;
        private bool _shown;
        private Action _onShown;

        event EventHandler IWindow.Focused
        {
            add { Activated += value; }
            remove { Activated -= value; }
        }

        event CancelableEventHandler IWindow.Closing
        {
            add { ClosingBackingEvent += value; }
            remove { ClosingBackingEvent -= value; }
        }

        private bool hasExistingMenu;
        private Menu menu;
        private string autosaveName = null;
        private readonly List<ToolStripMenuItem> shortcutItems = new List<ToolStripMenuItem>();
        private event CancelableEventHandler ClosingBackingEvent;

        public string Title
        {
            get { return Text; }
            set { Text = value; }
        }

        Size IWindow.Size
        {
            get { return ToLogicalUnits(new Size(Size.Width, Size.Height)); }
            set { Size = ToDeviceUnits(new SDSize((int)value.Width, (int)value.Height)); }
        }

        public Size MinSize
        {
            get { return ToLogicalUnits(new Size(MinimumSize.Width, MinimumSize.Height)); }
            set { MinimumSize = ToDeviceUnits(new SDSize((int)value.Width, (int)value.Height)); }
        }

        public Size MaxSize
        {
            get { return ToLogicalUnits(new Size(MaximumSize.Width, MaximumSize.Height)); }
            set { MaximumSize = ToDeviceUnits(new SDSize((int)value.Width, (int)value.Height)); }
        }

        public bool CanResize
        {
            get { return FormBorderStyle == FormBorderStyle.Sizable; }
            set
            {
                FormBorderStyle = value ? FormBorderStyle.Sizable : FormBorderStyle.FixedSingle;
                MaximizeBox = value;
            }
        }

        public string BackgroundColor
        {
            get { return ColorTools.ToHex(BackColor.R, BackColor.G, BackColor.B); }
            set
            {
                ColorTools.ParseHex(value, out byte r, out byte g, out byte b);
                BackColor = Color.FromArgb(r, g, b);
                webview.UpdateBackgroundColor(r, g, b);
            }
        }

        public bool UseBrowserTitle { get; set; }

        AppIcon IWindow.Icon
        {
            get { return icon; }
            set { SetIcon(value); }
        }

        public bool EnableScriptInterface
        {
            get { return webview.EnableScriptInterface; }
            set { webview.EnableScriptInterface = value; }
        }

        public bool EnableDevTools { get; set; }

        public Menu Menu
        {
            get
            {
                return menu;
            }
            set
            {
                SetMenu(value);
                menu = value;
            }
        }

        public IWebview Webview
        {
            get { return webview; }
        }

        public void ShowModal(IWindow modalWindow)
        {
            ((Form)modalWindow).ShowDialog(this);
        }

        object IWindow.NativeOptions => this;

        private readonly IWinFormsWebview webview;

        private AppIcon icon;

        public WinFormsWindow(WebviewBridge bridge)
        {
            if (bridge == null) { throw new ArgumentNullException(nameof(bridge)); }

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(LogicalDpi, LogicalDpi);

            webview = new WinFormsWebview(bridge);
            webview.Control.Location = new Point(0, 0);
            webview.Control.Dock = DockStyle.Fill;
            webview.Control.KeyDown += WebViewKeyDown;
            Controls.Add(webview.Control);
        }

        private void WebViewKeyDown(object sender, KeyEventArgs e)
        {
            // When the webview is focused, no key events arrive in the parent form (see https://github.com/MicrosoftEdge/WebView2Feedback/issues/468)
            // So this is a workaround to trigger all the shortcuts
            var pressedKeys = ModifierKeys | e.KeyCode;

            foreach (var shortcutItem in shortcutItems)
            {
                if (shortcutItem.ShortcutKeys == pressedKeys)
                {
                    shortcutItem.PerformClick();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
        }

        public void SetWindowState(WindowState state)
        {
            switch (state)
            {
                case SpiderEye.WindowState.Normal:
                    WindowState = FormWindowState.Normal;
                    break;

                case SpiderEye.WindowState.Maximized:
                    WindowState = FormWindowState.Maximized;
                    break;

                case SpiderEye.WindowState.Minimized:
                    WindowState = FormWindowState.Minimized;
                    break;

                default:
                    throw new ArgumentException($"Invalid window state of \"{state}\"", nameof(state));
            }
        }

        public void RestoreAndAutoSavePosition(string name, Size defaultSize)
        {
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
                StartPosition = FormStartPosition.CenterScreen;
                ((IWindow)this).Size = defaultSize;
                return;
            }

            var rect = new System.Drawing.Rectangle(
                savedInfo.Bounds.X,
                savedInfo.Bounds.Y,
                savedInfo.Bounds.Width,
                savedInfo.Bounds.Height);
            if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(rect)))
            {
                // Window would be out of bounds, fall back to the default size
                StartPosition = FormStartPosition.CenterScreen;
                ((IWindow)this).Size = defaultSize;
                return;
            }

            StartPosition = FormStartPosition.Manual;
            Location = new Point(savedInfo.Bounds.X, savedInfo.Bounds.Y);
            Size = new SDSize(savedInfo.Bounds.Width, savedInfo.Bounds.Height);

            // The DPI is not yet set to the correct monitor, wait until the for is shown
            ExecuteOnShown(() => Size = new SDSize(savedInfo.Bounds.Width, savedInfo.Bounds.Height));

            if (savedInfo.Maximised)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        public void SetIcon(AppIcon icon)
        {
            this.icon = icon;

            if (icon == null || icon.Icons.Length == 0) { Icon = null; }
            else
            {
                using var stream = icon.GetIconDataStream(icon.DefaultIcon);
                Icon = new Icon(stream);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _shown = true;
            _onShown?.Invoke();
            _onShown = null;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            SaveWindowInformation();

            var args = new CancelableEventArgs();
            ClosingBackingEvent?.Invoke(this, args);
            e.Cancel = args.Cancel;

            base.OnClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            webview.Dispose();
            base.Dispose(disposing);
        }

        // If the form has not been shown yet, wait for that.
        // Otherwise, WinForms will scale the form with the DPI from the primary monitor.
        // This is wrong when the form will be shown on another monitor with a different DPI setting.
        private void ExecuteOnShown(Action action)
        {
            if (!_shown)
            {
                _onShown = action;
            }
        }

        private void SetMenu(Menu menu)
        {
            shortcutItems.Clear();

            MenuStrip mainMenu;
            if (!hasExistingMenu)
            {
                mainMenu = new MenuStrip();
                MainMenuStrip = mainMenu;
                Controls.Add(MainMenuStrip);
                hasExistingMenu = true;

                MainMenuStrip.Click += MainMenuStrip_Click;
                MainMenuStrip.LostFocus += MainMenuStrip_LostFocus;
            }
            else
            {
                mainMenu = MainMenuStrip;
                mainMenu.Items.Clear();
            }

            if (menu == null)
            {
                return;
            }

            var nativeMenu = NativeCast.To<WinFormsMenu>(menu.NativeMenu).Menu;

            // the native menu behaves strange if you try to iterate over the items and add them to the menu
            // it almost behaves like a stack, calling mainMenu.Items.AddRange(nativeMenu.Menu.Items) throws because of this
            var menuItems = new List<ToolStripItem>();
            foreach (ToolStripItem i in nativeMenu.Items)
            {
                menuItems.Add(i);
                AddShortcutItems(i);
            }

            mainMenu.Items.AddRange(menuItems.ToArray());
        }

        private void MainMenuStrip_Click(object sender, EventArgs e)
        {
            // without this, the main menu strip never receives focus (focus is always on the WebView)
            MainMenuStrip.Focus();
        }

        private void MainMenuStrip_LostFocus(object sender, EventArgs e)
        {
            if (MainMenuStrip == null)
            {
                return;
            }

            // Workaround, the menu somehow is never hidden if the webview is clicked
            foreach (ToolStripItem item in MainMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDown && menuItem.DropDown.Visible)
                {
                    menuItem.DropDown.Hide();
                    break;
                }
            }
        }

        private void AddShortcutItems(ToolStripItem item)
        {
            if (item is not ToolStripMenuItem menuItem)
            {
                return;
            }

            if (menuItem.ShortcutKeys != Keys.None)
            {
                shortcutItems.Add(menuItem);
            }

            if (menuItem.HasDropDownItems)
            {
                foreach (ToolStripItem dropdownItem in menuItem.DropDownItems)
                {
                    AddShortcutItems(dropdownItem);
                }
            }
        }

        private SDSize ToDeviceUnits(SDSize logicalSize)
        {
            var logicalToDeviceUnitsScalingFactor = DeviceDpi / LogicalDpi;
            var scaledWidth = (int)Math.Round(logicalSize.Width * logicalToDeviceUnitsScalingFactor);
            var scaledHeight = (int)Math.Round(logicalSize.Height * logicalToDeviceUnitsScalingFactor);
            return new SDSize(scaledWidth, scaledHeight);
        }

        private Size ToLogicalUnits(Size deviceSize)
        {
            var deviceToLogicalUnitsScalingFactor = LogicalDpi / DeviceDpi;
            var scaledWidth = (int)Math.Round(deviceSize.Width * deviceToLogicalUnitsScalingFactor);
            var scaledHeight = (int)Math.Round(deviceSize.Height * deviceToLogicalUnitsScalingFactor);
            return new Size(scaledWidth, scaledHeight);
        }

        private void SaveWindowInformation()
        {
            if (autosaveName == null || Application.WindowInfoStorage == null)
            {
                return;
            }

            // When the window is minimized/maximised, take the bounds as if it was in a normal window state
            var bounds = WindowState == FormWindowState.Normal
                ? DesktopBounds
                : RestoreBounds;

            var windowInformation = new WindowInformation
            {
                Maximised = WindowState == FormWindowState.Maximized,
                Bounds = new Rectangle
                {
                    X = bounds.X,
                    Y = bounds.Y,
                    Height = bounds.Height,
                    Width = bounds.Width,
                },
            };
            Application.WindowInfoStorage.StoreWindowInformation(autosaveName, windowInformation);
        }
    }
}
