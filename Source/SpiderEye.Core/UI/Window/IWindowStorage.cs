namespace SpiderEye;

public interface IWindowStorage
{
    void StoreWindowInformation(string name, WindowInformation windowInformation);

    WindowInformation LoadWindowInformation(string name);
}
