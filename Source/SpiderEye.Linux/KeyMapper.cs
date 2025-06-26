using System;
using System.Collections.Generic;
using SpiderEye.Tools;

namespace SpiderEye.Linux;

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

    public static string ResolveShortcut(ModifierKey modifierKeys, Key key)
    {
        var modifiers = MapModifier(modifierKeys);
        var mappedKey = MapKey(key);
        return $"{string.Join(string.Empty, modifiers)}{mappedKey}";
    }

    private static string MapKey(Key key)
    {
        return key switch
        {
            Key.None => string.Empty,
            _ => key.ToString(),
        };
    }

    private static IEnumerable<string> MapModifier(ModifierKey modifier)
    {
        foreach (var flag in EnumTools.GetFlags(modifier))
        {
            switch (flag)
            {
                case ModifierKey.None:
                    continue;

                case ModifierKey.Shift:
                    yield return "<Shift>";
                    break;

                case ModifierKey.Primary:
                case ModifierKey.Control:
                    yield return "<Ctrl>";
                    break;

                case ModifierKey.Alt:
                    yield return "<Alt>";
                    break;

                case ModifierKey.Super:
                    yield return "<Super>";
                    break;

                default:
                    throw new NotSupportedException($"Unsupported modifier key: \"{flag}\"");
            }
        }
    }

    private static int StripKeyMask(Key key, KeyMask keyMask)
    {
        return (int)key & (~(int)keyMask);
    }
}
