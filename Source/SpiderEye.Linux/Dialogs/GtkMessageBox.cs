using System.Threading.Tasks;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal class GtkMessageBox : IMessageBox
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public MessageBoxButtons Buttons { get; set; }

        public Task<DialogResult> Show()
        {
            return Show(null);
        }

        public async Task<DialogResult> Show(IWindow parent)
        {
            var window = NativeCast.To<GtkWindow>(parent);
/* TODO not yet correctly supported by Gir.Core
            Gtk.Internal.AlertDialog.New()
            IntPtr dialog = IntPtr.Zero;
            try
            {
                using (GLibString title = Title)
                using (GLibString message = Message)
                {
                    dialog = Gtk.Dialog.CreateMessageDialog(
                       window?.Handle ?? IntPtr.Zero,
                       GtkDialogFlags.Modal | GtkDialogFlags.DestroyWithParent,
                       GtkMessageType.Other,
                       MapButtons(Buttons),
                       IntPtr.Zero);

                    GLib.SetProperty(dialog, "title", title);
                    GLib.SetProperty(dialog, "text", message);

                    var result = Gtk.Dialog.Run(dialog);
                    return MapResult(result);
                }
            }
            finally { if (dialog != IntPtr.Zero) { Gtk.Widget.Destroy(dialog); } }*/
            return DialogResult.Ok;
        }
    }
}
