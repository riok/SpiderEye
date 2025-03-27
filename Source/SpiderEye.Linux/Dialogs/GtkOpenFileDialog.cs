using System.Threading.Tasks;
using Gio;

namespace SpiderEye.Linux
{
    internal class GtkOpenFileDialog : GtkFileDialog, IOpenFileDialog
    {
        public bool Multiselect { get; set; }

        public string[] SelectedFiles { get; private set; }

        protected override async Task<DialogResult> ShowFileDialog(Gtk.FileDialog dialog, GtkWindow? parent)
        {
            // TODO dispose?
            var files = await dialog.OpenMultipleAsync(parent?.Window);

            if (files == null)
            {
                return DialogResult.Cancel;
            }

            var countOfFiles = files.GetNItems();
            SelectedFiles = new string[countOfFiles];
            for (uint i = 0; i < countOfFiles; i++)
            {
                var file = (File)files.GetObject(i);
                SelectedFiles[i] = file.GetPath();
            }

            return DialogResult.Ok;
        }
    }
}
