using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SpiderEye.Bridge;
using SpiderEye.Tools;
using SDSize = System.Drawing.Size;

namespace SpiderEye.Windows
{
    internal class WinFormsWindow : Form, IWindow
    {
        event CancelableEventHandler IWindow.Closing
        {
            add { ClosingBackingEvent += value; }
            remove { ClosingBackingEvent -= value; }
        }

        private bool hasExistingMenu;
        private Menu menu;
        private readonly List<ToolStripMenuItem> shortcutItems = new List<ToolStripMenuItem>();
        private event CancelableEventHandler ClosingBackingEvent;

        public string Title
        {
            get { return Text; }
            set { Text = value; }
        }

        Size IWindow.Size
        {
            get { return new Size(Size.Width, Size.Height); }
            set { Size = new SDSize((int)value.Width, (int)value.Height); }
        }

        public Size MinSize
        {
            get { return new Size(MinimumSize.Width, MinimumSize.Height); }
            set { MinimumSize = new SDSize((int)value.Width, (int)value.Height); }
        }

        public Size MaxSize
        {
            get { return new Size(MaximumSize.Width, MaximumSize.Height); }
            set { MaximumSize = new SDSize((int)value.Width, (int)value.Height); }
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

        protected override void OnClosing(CancelEventArgs e)
        {
            var args = new CancelableEventArgs();
            ClosingBackingEvent?.Invoke(this, args);
            e.Cancel = args.Cancel;

            base.OnClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            webview.Dispose();
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
    }
}
