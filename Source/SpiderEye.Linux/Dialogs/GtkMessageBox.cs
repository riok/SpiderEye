using System;
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
            using var dialog = new Gtk.MessageDialog();
            dialog.SetModal(window != null);
            dialog.SetDestroyWithParent(window != null);
            dialog.SetTransientFor(window?.Window);
            dialog.SetTitle(Title);
            dialog.Text = Message;
            dialog.SetApplication(LinuxApplication.App.NativeApplication);
            dialog.MessageType = Gtk.MessageType.Other;
            dialog.Decorated = true;

            switch (Buttons)
            {
                case MessageBoxButtons.Ok:
                    dialog.AddButton("OK", (int)DialogResult.Ok);
                    break;
                case MessageBoxButtons.OkCancel:
                    dialog.AddButton("OK", (int)DialogResult.Ok);
                    dialog.AddButton("Cancel", (int)DialogResult.Cancel);
                    break;
                case MessageBoxButtons.YesNo:
                    dialog.AddButton("Yes", (int)DialogResult.Yes);
                    dialog.AddButton("No", (int)DialogResult.No);
                    break;
                default:
                    dialog.AddButton("OK", (int)DialogResult.Ok);
                    break;
            }

            var taskResultSet = false;
            var tcs = new TaskCompletionSource<DialogResult>();
            dialog.OnResponse += (sender, args) =>
            {
                // OnResponse is called when the dialog is being closed, regardless if a button has been clicked
                // The tcs result may not be set here, as it is only set after closing the dialog.
                // This is because the dialog is disposed after the result is set, meaning we want to close it first and then dispose.
                if (taskResultSet)
                {
                    return;
                }

                taskResultSet = true;
                var result = (DialogResult)args.ResponseId;
                tcs.SetResult(Enum.IsDefined(result) ? result : DialogResult.None);
            };

            dialog.Show();
            var result = await tcs.Task;
            dialog.Destroy();
            return result;
        }
    }
}
