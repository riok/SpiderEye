using System.Threading.Tasks;
using SpiderEye.Tools;
using SpiderEye.Windows.Interop;

namespace SpiderEye.Windows
{
    internal abstract class WinFormsDialog<T> : IDialog
        where T : System.Windows.Forms.CommonDialog
    {
        public string Title { get; set; }

        public Task<DialogResult> Show()
        {
            return Show(null);
        }

        public Task<DialogResult> Show(IWindow parent)
        {
            var dialog = GetDialog();
            BeforeShow(dialog);

            var window = NativeCast.To<WinFormsWindow>(parent);
            var result = dialog.ShowDialog(window);

            BeforeReturn(dialog);

            return Task.FromResult(WinFormsMapper.MapResult(result));
        }

        protected abstract T GetDialog();

        protected virtual void BeforeShow(T dialog)
        {
        }

        protected virtual void BeforeReturn(T dialog)
        {
        }
    }
}
