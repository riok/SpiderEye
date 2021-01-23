using System;

namespace SpiderEye.Linux.Interop
{
    [Flags]
    internal enum GdkModifierType
    {
        // Taken from  https://api.gtkd.org/gdk.c.types.GdkModifierType.html
        None = 0,
        Shift = 1,
        Lock = 2,
        Control = 4,
        Mod1 = 8, // usually the Alt key
        Mod2 = 16,
        Mod3 = 32,
        Mod4 = 64,
        Mod5 = 128,
        Button1 = 256,
        Button2 = 512,
        Button3 = 1024,
        Button4 = 2048,
        Button5 = 4096,
        Super = 67108864,
        Hyper = 134217728,
        Meta = 268435456,
    }
}
