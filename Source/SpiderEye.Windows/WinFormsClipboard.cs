using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpiderEye.Windows;

internal class WinFormsClipboard : IClipboard
{
    public Task<string> GetText()
    {
        var clipboardContent = Application.Invoke(Clipboard.GetText);
        return Task.FromResult(clipboardContent);
    }

    public Task SetText(string text)
    {
        Application.Invoke(() => Clipboard.SetText(text));
        return Task.CompletedTask;
    }
}
