using System;
using System.Collections.Generic;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal class GtkSubMenu : IMenu
    {
        private bool hasSections;
        public List<GtkMenuItem> MenuItems { get; } = [];

        public void AddItem(IMenuItem item)
        {
            if (item == null) { throw new ArgumentNullException(nameof(item)); }

            var nativeItem = NativeCast.To<GtkMenuItem>(item);
            MenuItems.Add(nativeItem);
            hasSections |= item is GtkSeparatorMenuItem;
        }

        public Gio.Menu BuildMenu()
        {
            var menu = Gio.Menu.New();
            var currentMenu = menu;

            if (hasSections)
            {
                currentMenu = Gio.Menu.New();
                menu.AppendSection(null, currentMenu);
            }

            foreach (var item in MenuItems)
            {
                if (item is GtkLabelMenuItem labelItem)
                {
                    labelItem.AddToMenu(currentMenu);
                }
                else
                {
                    currentMenu = Gio.Menu.New();
                    menu.AppendSection(null, currentMenu);
                }
            }

            return menu;
        }

        public void AddToActionGroup(Gio.SimpleActionGroup actionGroup)
        {
            foreach (var item in MenuItems)
            {
                if (item is GtkLabelMenuItem labelItem)
                {
                    labelItem.AddToActionGroup(actionGroup);
                }
            }
        }

        public void Dispose()
        {
            foreach (var item in MenuItems)
            {
                item.Dispose();
            }
        }
    }
}
