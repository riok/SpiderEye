using SpiderEye.Bridge;

namespace SpiderEye.Linux
{
    internal class GtkUiFactory : IUiFactory
    {
        public IWindow CreateWindow(WindowConfiguration config, WebviewBridge bridge)
        {
            return new GtkWindow(bridge);
        }

        public IMessageBox CreateMessageBox()
        {
            return new GtkMessageBox();
        }

        public ISaveFileDialog CreateSaveFileDialog()
        {
            return new GtkSaveFileDialog();
        }

        public IOpenFileDialog CreateOpenFileDialog()
        {
            return new GtkOpenFileDialog();
        }

        public IFolderSelectDialog CreateFolderSelectDialog()
        {
            return new GtkSelectFolderDialog();
        }

        public IMenu CreateMenu()
        {
            return new GtkTopMenu();
        }

        public ILabelMenuItem CreateLabelMenu(string label)
        {
            return new GtkLabelMenuItem(label);
        }

        public IMenuItem CreateMenuSeparator()
        {
            return new GtkSeparatorMenuItem();
        }
    }
}
