using System.Threading.Tasks;

namespace SpiderEye.Linux
{
    internal class GtkSaveFileDialog : GtkFileDialog, ISaveFileDialog
    {
        public bool OverwritePrompt { get; set; }

        protected override async Task<DialogResult> ShowFileDialog(Gtk.FileDialog dialog, GtkWindow? parent)
        {
            var file = await dialog.SaveAsync(parent?.Window);
            FileName = file?.GetPath();
            return file == null
                ? DialogResult.Cancel
                : DialogResult.Ok;
        }
    }
}
