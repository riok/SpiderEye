namespace SpiderEye.Linux;

internal abstract class GtkMenuItem : IMenuItem
{
    public virtual void Dispose()
    {
        // Nothing to do
    }

    public abstract IMenu CreateSubMenu();
}
