using System;

namespace SpiderEye.Linux
{
    internal class GtkMenuItem : IMenuItem
    {
        public readonly IntPtr Handle;
        protected GtkSubMenu subMenu;

        protected GtkMenuItem(IntPtr handle)
        {
            Handle = handle;
        }

        public IMenu CreateSubMenu()
        {
            if (subMenu == null)
            {
                subMenu = new GtkSubMenu(Handle);
            }

            return subMenu;
        }

        public virtual void SetAccelGroup(IntPtr accelGroupHandle)
        {
        }

        public void Dispose()
        {
            // TODO Gtk.Widget.Destroy(Handle);
        }
    }
}
