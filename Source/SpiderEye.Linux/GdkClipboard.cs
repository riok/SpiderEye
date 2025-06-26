using System.Threading.Tasks;
using Gdk;

namespace SpiderEye.Linux;

internal class GdkClipboard : IClipboard
{
    private Clipboard clipboard;

    public Task<string> GetText()
    {
        EnsureClipboard();
        return clipboard.ReadTextAsync();
    }

    public Task SetText(string text)
    {
        EnsureClipboard();
        clipboard.SetText(text);
        return Task.CompletedTask;
    }

    private void EnsureClipboard()
    {
        // Wait with creating a clipboard until the app is ready
        clipboard ??= Display.GetDefault()?.GetClipboard();
    }
}
