using Gtk;

namespace SpiderEye.Linux
{
    internal class GtkSaveFileDialog : GtkFileDialog, ISaveFileDialog
    {
        protected override FileChooserAction Type => FileChooserAction.Save;
        public bool OverwritePrompt { get; set; } // No-op
    }
}
