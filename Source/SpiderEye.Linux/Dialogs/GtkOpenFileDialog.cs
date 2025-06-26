using System;
using Gtk;

namespace SpiderEye.Linux
{
    internal class GtkOpenFileDialog : GtkFileDialog, IOpenFileDialog
    {
        protected override FileChooserAction Type => FileChooserAction.Open;

        public bool Multiselect { get; set; }

        public string[] SelectedFiles { get; private set; }

        protected override void BeforeShow(FileChooserNative dialog)
        {
            base.BeforeShow(dialog);
            dialog.SetSelectMultiple(Multiselect);
        }

        protected override void BeforeReturn(FileChooserNative dialog, DialogResult result)
        {
            base.BeforeReturn(dialog, result);

            using var files = dialog.GetFiles();
            SelectedFiles = new string[files.GetNItems()];
            for (uint i = 0; i < SelectedFiles.Length; i++)
            {
                SelectedFiles[i] = (files.GetObject(i) as Gio.FileHelper)?.GetPath() ?? string.Empty;
            }
        }
    }
}
