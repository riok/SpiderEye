using System.Threading.Tasks;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal abstract class GtkDialog : IDialog
    {
        public string Title { get; set; }

        public Task<DialogResult> Show()
        {
            return Show(null);
        }

        public async Task<DialogResult> Show(IWindow parent)
        {
            var window = NativeCast.To<GtkWindow>(parent);
            using var chooser = Gtk.FileDialog.New();
            chooser.SetTitle(Title);

            return await Show(chooser, window);
        }

        protected abstract Task<DialogResult> Show(Gtk.FileDialog dialog, GtkWindow parent);
    }
}
