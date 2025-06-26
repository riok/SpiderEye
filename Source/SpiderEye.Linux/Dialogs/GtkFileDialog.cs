using System.Collections.Generic;
using Gtk;
using Functions = Gio.Functions;

namespace SpiderEye.Linux
{
    internal abstract class GtkFileDialog : GtkDialog, IFileDialog
    {
        public string InitialDirectory { get; set; }
        public string FileName { get; set; }
        public ICollection<FileFilter> FileFilters { get; }

        protected GtkFileDialog()
        {
            FileFilters = new List<FileFilter>();
        }

        protected override void BeforeShow(FileChooserNative dialog)
        {
            if (!string.IsNullOrWhiteSpace(InitialDirectory))
            {
                using var initialDir = Functions.FileNewForPath(InitialDirectory);
                dialog.SetCurrentFolder(initialDir);
            }

            if (!string.IsNullOrWhiteSpace(FileName))
            {
                dialog.SetCurrentName(FileName);
            }

            SetFileFilters(dialog, FileFilters);
        }

        protected override void BeforeReturn(FileChooserNative dialog, DialogResult result)
        {
            if (result == DialogResult.Ok)
            {
                using var file = dialog.GetFile();
                FileName = file?.GetPath();
            }
            else { FileName = null; }
        }

        private void SetFileFilters(FileChooserNative dialog, IEnumerable<FileFilter> filters)
        {
            foreach (var filter in filters)
            {
                var f = Gtk.FileFilter.New();
                f.SetName(filter.Name);

                foreach (string filterValue in filter.Filters)
                {
                    f.AddPattern(filterValue);
                }

                dialog.AddFilter(f);
            }
        }
    }
}
