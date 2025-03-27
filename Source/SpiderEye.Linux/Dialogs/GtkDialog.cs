using System;
using System.Threading.Tasks;
using Gtk;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal abstract class GtkDialog : IDialog
    {
        public string Title { get; set; }

        protected abstract FileChooserAction Type { get; }

        public Task<DialogResult> Show()
        {
            return Show(null);
        }

        public async Task<DialogResult> Show(IWindow parent)
        {
            var window = NativeCast.To<GtkWindow>(parent);
            using var chooser = FileChooserNative.New(
                Title,
                window?.Window,
                Type,
                GetAcceptString(Type),
                "_Cancel");
            chooser.SetTitle(Title);
            chooser.SetCreateFolders(true);


            var taskResultSet = false;
            var tcs = new TaskCompletionSource<DialogResult>();
            chooser.OnResponse += (sender, args) =>
            {
                // OnResponse is called when the dialog is being closed, regardless if a button has been clicked
                // The tcs result may not be set here, as it is only set after closing the dialog.
                // This is because the dialog is disposed after the result is set, meaning we want to close it first and then dispose.
                if (taskResultSet)
                {
                    return;
                }

                taskResultSet = true;
                var result = MapResult((ResponseType)args.ResponseId);
                tcs.SetResult(result);
            };

            chooser.Show();

            var result = await tcs.Task;
            BeforeReturn(chooser, result);
            chooser.Destroy();
            return result;
        }

        protected virtual void BeforeShow(FileChooserNative dialog)
        {
        }

        protected virtual void BeforeReturn(FileChooserNative dialog, DialogResult result)
        {
        }

        private string GetAcceptString(FileChooserAction type)
        {
            return type switch
            {
                FileChooserAction.Open => "_Open",
                FileChooserAction.Save => "_Save",
                FileChooserAction.SelectFolder => "_Select",
                _ => "_Select"
            };
        }

        private DialogResult MapResult(ResponseType result)
        {
            switch (result)
            {
                case ResponseType.Accept:
                case ResponseType.Ok:
                case ResponseType.Yes:
                case ResponseType.Apply:
                    return DialogResult.Ok;

                case ResponseType.Reject:
                case ResponseType.Cancel:
                case ResponseType.Close:
                case ResponseType.No:
                    return DialogResult.Cancel;

                default:
                    return DialogResult.None;
            }
        }
    }
}
