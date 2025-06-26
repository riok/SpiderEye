namespace SpiderEye.Linux
{
    internal class GtkSeparatorMenuItem : GtkMenuItem
    {
        public override IMenu CreateSubMenu()
        {
            // Cannot create a sub menu on a separator item
            return null;
        }
    }
}
