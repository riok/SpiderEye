namespace SpiderEye.Mac
{
    public abstract partial class CocoaBaseMenu
    {
        /// <summary>
        /// Adds the view sub menu.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu.</returns>
        public MenuItem AddView(MenuItemCollection menuItems)
        {
            var view = menuItems.AddLabelItem("View");
            AddEnterFullScreen(view.MenuItems);
            return view;
        }

        /// <summary>
        /// Adds a menu item for the fullscreen toggle action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddEnterFullScreen(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Enter Full Screen", "toggleFullScreen:", ModifierKey.Super | ModifierKey.Control, Key.F);
        }
    }
}
