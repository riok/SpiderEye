namespace SpiderEye
{
    internal interface IMacOsMenuCollection
    {
        MenuItem AddApp(MenuItemCollection menuItems);

        LabelMenuItem AddAbout(MenuItemCollection menuItems);

        LabelMenuItem AddHide(MenuItemCollection menuItems);

        LabelMenuItem AddHideOtherApplications(MenuItemCollection menuItems);

        LabelMenuItem AddUnhideAllApplications(MenuItemCollection menuItems);

        LabelMenuItem AddQuit(MenuItemCollection menuItems);

        MenuItem AddEdit(MenuItemCollection menuItems);

        LabelMenuItem AddUndo(MenuItemCollection menuItems);

        LabelMenuItem AddRedo(MenuItemCollection menuItems);

        LabelMenuItem AddCut(MenuItemCollection menuItems);

        LabelMenuItem AddCopy(MenuItemCollection menuItems);

        LabelMenuItem AddPaste(MenuItemCollection menuItems);

        LabelMenuItem AddSelectAll(MenuItemCollection menuItems);

        MenuItem AddView(MenuItemCollection menuItems);

        LabelMenuItem AddEnterFullScreen(MenuItemCollection menuItems);

        MenuItem AddWindow(MenuItemCollection menuItems);

        LabelMenuItem AddMinimize(MenuItemCollection menuItems);

        LabelMenuItem AddZoom(MenuItemCollection menuItems);

        LabelMenuItem AddBringAllToFront(MenuItemCollection menuItems);
    }
}
