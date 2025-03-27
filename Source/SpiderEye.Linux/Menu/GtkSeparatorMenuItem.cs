using System;

namespace SpiderEye.Linux
{
    internal class GtkSeparatorMenuItem : GtkMenuItem
    {
        public GtkSeparatorMenuItem()
            // TODO : base(Gtk.Menu.CreateSeparatorItem())
        : base(IntPtr.Zero)
        {
        }
    }
}
