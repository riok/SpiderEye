using System;
using System.Collections.Generic;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal class GtkMenu : IMenu
    {
        private readonly List<GtkMenuItem> menuItems = new List<GtkMenuItem>();

        public void AddItem(IMenuItem item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            var nativeItem = NativeCast.To<GtkMenuItem>(item);
            menuItems.Add(nativeItem);
        }

        public IEnumerable<GtkMenuItem> GetItems()
        {
            return menuItems;
        }

        public void SetAccelGroup(IntPtr handle)
        {
            foreach (var item in menuItems)
            {
                item.SetAccelGroup(handle);
            }
        }

        public void Dispose()
        {
            foreach (var item in menuItems)
            {
                item.Dispose();
            }
        }
    }
}
