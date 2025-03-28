using System.Threading.Tasks;

namespace SpiderEye.Playground.Core.Bridge;

public interface IUiBridgeClientService
{
    void ShowMessage(string message);

    Task<string> Prompt(string message);
}
