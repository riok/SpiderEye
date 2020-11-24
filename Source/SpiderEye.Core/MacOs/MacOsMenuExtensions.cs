namespace SpiderEye
{
    public static class MacOsMenuExtensions
    {
        public static MenuItem AddMacOsApp(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddApp(menuItems);

        public static LabelMenuItem AddMacOsAbout(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddAbout(menuItems);
        public static LabelMenuItem AddMacOsHide(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddHide(menuItems);

        public static LabelMenuItem AddMacOsHideOtherApplications(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddHideOtherApplications(menuItems);

        public static LabelMenuItem AddMacOsUnhideAllApplications(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddUnhideAllApplications(menuItems);

        public static LabelMenuItem AddMacOsQuit(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddQuit(menuItems);

        public static MenuItem AddMacOsEdit(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddEdit(menuItems);

        public static LabelMenuItem AddMacOsUndo(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddUndo(menuItems);

        public static LabelMenuItem AddMacOsRedo(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddRedo(menuItems);

        public static LabelMenuItem AddMacOsCut(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddCut(menuItems);

        public static LabelMenuItem AddMacOsCopy(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddCopy(menuItems);

        public static LabelMenuItem AddMacOsPaste(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddPaste(menuItems);

        public static LabelMenuItem AddMacOsSelectAll(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddSelectAll(menuItems);

        public static MenuItem AddMacOsView(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddView(menuItems);

        public static LabelMenuItem AddMacOsEnterFullScreen(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddEnterFullScreen(menuItems);

        public static MenuItem AddMacOsWindow(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddWindow(menuItems);

        public static LabelMenuItem AddMacOsMinimize(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddMinimize(menuItems);

        public static LabelMenuItem AddMacOsZoom(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddZoom(menuItems);

        public static LabelMenuItem AddMacOsBringAllToFront(this MenuItemCollection menuItems)
            => menuItems.MacOs?.AddBringAllToFront(menuItems);

        public static void AddMacOsSeparator(this MenuItemCollection menuItems)
        {
            if (menuItems.MacOs != null)
            {
                menuItems.AddSeparatorItem();
            }
        }
    }
}
