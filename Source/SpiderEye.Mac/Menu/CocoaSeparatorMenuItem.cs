using SpiderEye.Mac.Native;

namespace SpiderEye.Mac
{
    internal class CocoaSeparatorMenuItem : CocoaMenuItem
    {
        public CocoaSeparatorMenuItem()
            : base(Native.AppKit.Call("NSMenuItem", "separatorItem"))
        {
        }
    }
}
