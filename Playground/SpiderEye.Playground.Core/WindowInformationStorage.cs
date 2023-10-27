using System.IO;
using System.Text.Json;

namespace SpiderEye.Playground.Core;

public class WindowInformationStorage : IWindowStorage
{
    private const string Path = "./storage.json";

    public void StoreWindowInformation(string name, WindowInformation windowInformation)
    {
        using var fs = File.Open(Path, FileMode.Create);
        JsonSerializer.Serialize(fs, windowInformation);
    }

    public WindowInformation LoadWindowInformation(string name)
    {
        if (!File.Exists(Path))
        {
            return null;
        }

        using var fs = File.OpenRead(Path);
        return JsonSerializer.Deserialize<WindowInformation>(fs);
    }
}
