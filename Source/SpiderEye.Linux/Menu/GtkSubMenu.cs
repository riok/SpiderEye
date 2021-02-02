using System;
using System.Collections.Generic;
using SpiderEye.Linux.Native;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal class GtkSubMenu : IMenu
    {
        private readonly IntPtr parentMenuItem;
        private readonly List<GtkMenuItem> menuItems = new List<GtkMenuItem>();
        private IntPtr? handle;

        internal GtkSubMenu(IntPtr parentMenuItem)
        {
            this.parentMenuItem = parentMenuItem;
        }

        public void AddItem(IMenuItem item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            if (handle == null)
            {
                handle = Gtk.Menu.Create();
                Gtk.Menu.SetSubmenu(parentMenuItem, handle.Value);
            }

            var nativeItem = NativeCast.To<GtkMenuItem>(item);
            menuItems.Add(nativeItem);
            Gtk.Menu.AddItem(handle.Value, nativeItem.Handle);
            Gtk.Widget.Show(nativeItem.Handle);
        }


        public void SetAccelGroup(IntPtr accelGroupHandle)
        {
            if (handle == null)
            {
                return;
            }

            Gtk.Menu.SetAccelGroup(handle.Value, accelGroupHandle);
            foreach (var item in menuItems)
            {
                item.SetAccelGroup(accelGroupHandle);
            }
        }

        public void Dispose()
        {
            if (handle != null)
            {
                Gtk.Widget.Destroy(handle.Value);
            }
        }
    }
}
