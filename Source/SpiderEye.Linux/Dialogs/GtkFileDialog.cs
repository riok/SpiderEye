using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gio;

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

        protected override async Task<DialogResult> Show(Gtk.FileDialog dialog, GtkWindow? parent)
        {
            if (!string.IsNullOrWhiteSpace(InitialDirectory))
            {
                var initialFolder = Functions.FileNewForPath(InitialDirectory);
                dialog.SetInitialFolder(initialFolder);
            }

            if (!string.IsNullOrWhiteSpace(FileName))
            {
                dialog.SetInitialName(FileName);
            }

            using var _ = SetFileFilters(dialog, FileFilters);
            return await ShowFileDialog(dialog, parent);
        }

        protected virtual async Task<DialogResult> ShowFileDialog(Gtk.FileDialog dialog, GtkWindow? parent)
        {
            var file = await dialog.OpenAsync(parent?.Window);
            FileName = file?.GetPath();
            return file == null
                ? DialogResult.Cancel
                : DialogResult.Ok;
        }

        private IDisposable? SetFileFilters(Gtk.FileDialog dialog, IEnumerable<FileFilter> filters)
        {
            if (!filters.Any()) { return null; }

            var filterList = ListStore.New(Gtk.FileFilter.GetGType());

            foreach (var filter in filters)
            {
                var f = Gtk.FileFilter.New();
                f.SetName(filter.Name);

                foreach (string filterValue in filter.Filters)
                {
                    f.AddPattern(filterValue);
                }

                filterList.Append(f);
            }

            dialog.SetFilters(filterList);
            return filterList;
        }
    }
}
