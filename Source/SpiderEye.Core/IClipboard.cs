using System.Threading.Tasks;

namespace SpiderEye;

public interface IClipboard
{
    Task<string> GetText();

    Task SetText(string text);
}
