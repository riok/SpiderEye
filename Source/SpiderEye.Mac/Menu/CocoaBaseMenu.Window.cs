namespace SpiderEye.Mac
{
    public abstract partial class CocoaBaseMenu
    {
        /// <summary>
        /// Adds the window sub menu.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu.</returns>
        public MenuItem AddWindow(MenuItemCollection menuItems)
        {
            var edit = menuItems.AddLabelItem("Window");
            AddMinimize(edit.MenuItems);
            AddZoom(edit.MenuItems);
            AddBringAllToFront(edit.MenuItems);
            return edit;
        }

        /// <summary>
        /// Adds a menu item for the window minimize action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddMinimize(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Minimize", "performMiniaturize:", ModifierKey.Super, Key.M);
        }

        /// <summary>
        /// Adds a menu item for the window zoom action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddZoom(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Zoom", "performZoom:");
        }

        /// <summary>
        /// Adds a menu item for the bring windows to front action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddBringAllToFront(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Bring All to Front", "arrangeInFront:");
        }
    }
}
