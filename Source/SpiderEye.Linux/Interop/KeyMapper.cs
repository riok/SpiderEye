using System;
using SpiderEye.Linux.Interop;
using SpiderEye.Tools;

namespace SpiderEye.UI.Platforms.Linux.Interop
{
    internal static class KeyMapper
    {
        public static (ModifierKey ModifierKey, Key Key) ResolveSystemShortcut(SystemShortcut systemShortcut)
        {
            return systemShortcut switch
            {
                SystemShortcut.Close => (ModifierKey.Control, Key.Q),
                SystemShortcut.Help => (ModifierKey.None, Key.F1),
                _ => throw new NotSupportedException($"Unsupported system shortcut: \"{systemShortcut}\""),
            };
        }

        public static uint MapKey(Key key)
        {
            if (key >= Key.F1 && key <= Key.F12)
            {
                uint f1 = 0xFFBE;
                var fKey = StripKeyMask(key, KeyMask.Function);
                return f1 + (uint)fKey - 1;
            }

            if (key >= Key.Number0 && key <= Key.Number9)
            {
                uint key0 = 0x030;
                var number = StripKeyMask(key, KeyMask.Number);
                if (number == 10)
                {
                    number = 0;
                }

                return key0 + (uint)number;
            }

            if (key >= Key.A && key <= Key.Z)
            {
                uint a = 0x061;
                var number = StripKeyMask(key, KeyMask.Alphabet);
                return a + (uint)number - 1;
            }

            return 0;
        }

        public static GdkModifierType MapModifier(ModifierKey modifier)
        {
            var modifierType = GdkModifierType.None;

            foreach (var flag in EnumTools.GetFlags(modifier))
            {
                switch (flag)
                {
                    case ModifierKey.None:
                        continue;

                    case ModifierKey.Shift:
                        modifierType |= GdkModifierType.Shift;
                        break;

                    case ModifierKey.Primary:
                    case ModifierKey.Control:
                        modifierType |= GdkModifierType.Control;
                        break;

                    case ModifierKey.Alt:
                        modifierType |= GdkModifierType.Mod1;
                        break;

                    case ModifierKey.Super:
                        modifierType |= GdkModifierType.Super;
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported modifier key: \"{flag}\"");
                }
            }

            return modifierType;
        }

        private static int StripKeyMask(Key key, KeyMask keyMask)
        {
            return (int)key & (~(int)keyMask);
        }
    }
}
