namespace SpiderEye.Mac
{
    public abstract partial class CocoaBaseMenu
    {
        /// <summary>
        /// Adds the edit sub menu.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu.</returns>
        public MenuItem AddApp(MenuItemCollection menuItems)
        {
            var app = menuItems.AddLabelItem(string.Empty);
            AddAbout(app.MenuItems);
            app.MenuItems.AddSeparatorItem();
            AddHide(app.MenuItems);
            AddHideOtherApplications(app.MenuItems);
            AddUnhideAllApplications(app.MenuItems);
            app.MenuItems.AddSeparatorItem();
            AddQuit(app.MenuItems);
            return app;
        }

        /// <summary>
        /// Adds a menu item for the open default about panel action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddAbout(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "About", "orderFrontStandardAboutPanel:");
        }

        /// <summary>
        /// Adds the services menu.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddHide(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Hide", "hide:", ModifierKey.Super, Key.H);
        }

        /// <summary>
        /// Adds a menu item for the hide application action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddHideOtherApplications(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Hide Others", "hideOtherApplications:", ModifierKey.Super | ModifierKey.Alt, Key.H);
        }

        /// <summary>
        /// Adds a menu item for the hide other applications action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddUnhideAllApplications(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Show All", "unhideAllApplications:");
        }

        /// <summary>
        /// Adds a menu item for the quit applications action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddQuit(MenuItemCollection menuItems)
        {
            var item = menuItems.AddLabelItem("Quit");
            item.SetShortcut(ModifierKey.Super, Key.Q);
            item.Click += (s, e) => Application.Exit();

            return item;
        }
    }
}
