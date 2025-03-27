using System;

namespace SpiderEye;

// Copy-paste of Gio.ApplicationFlags
// Cannot use Gio.ApplicationFlags directly here, as we do not want to reference that library from the Core
[Flags]
public enum LinuxApplicationFlags : uint
{
    FlagsNone = 0,
    DefaultFlags = 0,
    IsService = 1,
    IsLauncher = 2,
    HandlesOpen = 4,
    HandlesCommandLine = 8,
    SendEnvironment = 16,
    NonUnique = 32,
    CanOverrideAppId = 64,
    AllowReplacement = 128,
    Replace = 256,
}
