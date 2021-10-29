using System;
using SpiderEye.Mac.Native;

namespace SpiderEye.Mac.Interop
{
    /// <summary>
    /// Names of NSAppearanceName.
    /// </summary>
    public static class NSAppearanceName
    {
        private const string NameVibrantDark = "NSAppearanceNameVibrantDark";
        private const string NameVibrantLight = "NSAppearanceNameVibrantLight";
        private const string NameDarkAqua = "NSAppearanceNameDarkAqua";
        private const string NameAqua = "NSAppearanceNameAqua";

        /// <summary>
        /// NSAppearanceNameVibrantDark.
        /// </summary>
        public static readonly IntPtr NSAppearanceNameVibrantDark = NSString.Create(NameVibrantDark);

        /// <summary>
        /// NSAppearanceNameVibrantLight.
        /// </summary>
        public static readonly IntPtr NSAppearanceNameVibrantLight = NSString.Create(NameVibrantLight);

        /// <summary>
        /// NSAppearanceNameDarkAqua.
        /// </summary>
        public static readonly IntPtr NSAppearanceNameDarkAqua = NSString.Create(NameDarkAqua);

        /// <summary>
        /// NSAppearanceNameAqua.
        /// </summary>
        public static readonly IntPtr NSAppearanceNameAqua = NSString.Create(NameAqua);

        internal static IntPtr GetNSAppearance(MacOsAppearance? value)
        {
            if (value == null)
            {
                return IntPtr.Zero;
            }

            var appearanceName = value switch
            {
                MacOsAppearance.Aqua => NSAppearanceNameAqua,
                MacOsAppearance.DarkAqua => NSAppearanceNameDarkAqua,
                MacOsAppearance.VibrantLight => NSAppearanceNameVibrantLight,
                MacOsAppearance.VibrantDark => NSAppearanceNameVibrantDark,
                _ => throw new InvalidOperationException("unsupported appearance"),
            };

            return AppKit.Call("NSAppearance", "appearanceNamed:", appearanceName);
        }

        internal static MacOsAppearance GetMacOsAppearance(IntPtr nsAppearance)
        {
            var name = NSString.GetString(ObjC.Call(nsAppearance, "name"));
            return name switch
            {
                NameDarkAqua => MacOsAppearance.DarkAqua,
                NameVibrantDark => MacOsAppearance.VibrantDark,
                NameVibrantLight => MacOsAppearance.VibrantLight,
                _ => MacOsAppearance.Aqua
            };
        }
    }
}
