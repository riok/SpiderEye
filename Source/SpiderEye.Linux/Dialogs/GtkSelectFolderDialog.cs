using Gtk;
using Functions = Gio.Functions;

namespace SpiderEye.Linux
{
    internal class GtkSelectFolderDialog : GtkDialog, IFolderSelectDialog
    {
        protected override FileChooserAction Type => FileChooserAction.SelectFolder;
        public string SelectedPath { get; set; }

        protected override void BeforeShow(FileChooserNative dialog)
        {
            if (!string.IsNullOrWhiteSpace(SelectedPath))
            {
                using var initialDir = Functions.FileNewForPath(SelectedPath);
                dialog.SetCurrentFolder(initialDir);
            }
        }

        protected override unsafe void BeforeReturn(FileChooserNative dialog, DialogResult result)
        {
            if (result == DialogResult.Ok)
            {
                using var file = dialog.GetFile();
                SelectedPath = file?.GetPath();
            }
            else { SelectedPath = null; }
        }
    }
}
