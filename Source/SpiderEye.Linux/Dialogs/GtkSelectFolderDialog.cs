using System.Threading.Tasks;
using Gio;

namespace SpiderEye.Linux
{
    internal class GtkSelectFolderDialog : GtkDialog, IFolderSelectDialog
    {
        public string SelectedPath { get; set; }

        protected override async Task<DialogResult> Show(Gtk.FileDialog dialog, GtkWindow parent)
        {
            if (!string.IsNullOrWhiteSpace(SelectedPath))
            {
                var initialFolder = Functions.FileNewForPath(SelectedPath);
                dialog.SetInitialFolder(initialFolder);
            }

            var folder = await dialog.SelectFolderAsync(parent?.Window);
            SelectedPath = folder?.GetPath();
            return folder == null
                ? DialogResult.Cancel
                : DialogResult.Ok;
        }
    }
}
