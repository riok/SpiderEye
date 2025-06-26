using System;
using System.Collections.Generic;

namespace SpiderEye.Linux
{
    internal class GtkTopMenu : IMenu
    {
        internal const string MenuActionPrefix = "menu";
        private readonly List<GtkLabelMenuItem> menuItems = [];

        public void AddItem(IMenuItem item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            if (item is not GtkLabelMenuItem labelMenuItem)
            {
                throw new NotSupportedException("Only label menu items are supported.");
            }

            menuItems.Add(labelMenuItem);
        }

        public Gio.Menu BuildMenu()
        {
            var topMenu = Gio.Menu.New();
            foreach (var item in menuItems)
            {
                item.AddToMenu(topMenu);
            }

            return topMenu;
        }

        public Gio.ActionGroup BuildActionGroup()
        {
            var actionGroup = Gio.SimpleActionGroup.New();
            foreach (var item in menuItems)
            {
                item.AddToActionGroup(actionGroup);
            }

            return actionGroup;
        }

        public void Dispose()
        {
            foreach (var menuItem in menuItems)
            {
                menuItem.Dispose();
            }
        }
    }
}
