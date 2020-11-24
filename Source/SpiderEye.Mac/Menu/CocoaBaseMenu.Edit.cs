namespace SpiderEye.Mac
{
    public abstract partial class CocoaBaseMenu
    {
        /// <summary>
        /// Adds the edit sub menu.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu.</returns>
        public MenuItem AddEdit(MenuItemCollection menuItems)
        {
            var edit = menuItems.AddLabelItem("Edit");
            AddUndo(edit.MenuItems);
            AddRedo(edit.MenuItems);
            edit.MenuItems.AddSeparatorItem();
            AddCut(edit.MenuItems);
            AddCopy(edit.MenuItems);
            AddPaste(edit.MenuItems);
            AddSelectAll(edit.MenuItems);
            return edit;
        }

        /// <summary>
        /// Adds a menu item for the undo action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddUndo(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Undo", "undo:", ModifierKey.Super, Key.Z);
        }

        /// <summary>
        /// Adds a menu item for the redo action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddRedo(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Redo", "redo:", ModifierKey.Super | ModifierKey.Shift, Key.Z);
        }

        /// <summary>
        /// Adds a menu item for the cut text action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddCut(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Cut", "cut:", ModifierKey.Super, Key.X);
        }

        /// <summary>
        /// Adds a menu item for the copy text action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddCopy(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Copy", "copy:", ModifierKey.Super, Key.C);
        }

        /// <summary>
        /// Adds a menu item for the paste text action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddPaste(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Paste", "paste:", ModifierKey.Super, Key.V);
        }

        /// <summary>
        /// Adds a menu item for the select all text action.
        /// </summary>
        /// <param name="menuItems">The menu item collection to add this menu item to.</param>
        /// <returns>The created menu item.</returns>
        public LabelMenuItem AddSelectAll(MenuItemCollection menuItems)
        {
            return AddDefaultHandlerMenuItem(menuItems, "Select All", "selectAll:", ModifierKey.Super, Key.A);
        }
    }
}
