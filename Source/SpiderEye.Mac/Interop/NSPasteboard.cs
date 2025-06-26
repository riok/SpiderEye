using System;
using SpiderEye.Mac.Native;

namespace SpiderEye.Mac.Interop
{
    internal static class NSPasteboard
    {
        public static IntPtr Get()
        {
            return ObjC.Call(
                ObjC.GetClass("NSPasteboard"),
                "generalPasteboard");
        }
    }
}
