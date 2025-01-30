using System;
using System.Threading;
using SpiderEye.Mac.Interop;
using SpiderEye.Mac.Native;

namespace SpiderEye.Mac
{
    internal class CocoaApplication : IApplication, IMacOsApplicationOptions
    {
        public IUiFactory Factory { get; }

        public SynchronizationContext SynchronizationContext { get; }

        public IntPtr Handle { get; }

        public bool? IsDarkModeEnabled => EffectiveAppearance is MacOsAppearance.DarkAqua or MacOsAppearance.VibrantDark;

        object IApplication.NativeOptions => this;

        private static readonly NativeClassDefinition AppDelegateDefinition;
        private readonly NativeClassInstance appDelegate;
        private MacOsAppearance? macOsAppearanceField;
        private bool titleBarTransparentField;

        public MacOsAppearance? Appearance
        {
            get => macOsAppearanceField;
            set
            {
                if (macOsAppearanceField == value)
                {
                    return;
                }

                macOsAppearanceField = value;
                var appearance = NSAppearanceName.GetNSAppearance(value);
                ObjC.Call(Handle, "setAppearance:", appearance);
            }
        }

        public MacOsAppearance EffectiveAppearance
        {
            get
            {
                var appearance = ObjC.GetProperty(Handle, "effectiveAppearance");
                return NSAppearanceName.GetMacOsAppearance(appearance);
            }
        }

        public bool TransparentTitleBar
        {
            get => titleBarTransparentField;
            set
            {
                if (titleBarTransparentField == value)
                {
                    return;
                }

                titleBarTransparentField = value;
                ObjC.SetProperty(Handle, "titlebarAppearsTransparent", value);
            }
        }

        static CocoaApplication()
        {
            AppDelegateDefinition = CreateAppDelegate();
        }

        public CocoaApplication()
        {
            Factory = new CocoaUiFactory();
            SynchronizationContext = new CocoaSynchronizationContext();

            Handle = AppKit.Call("NSApplication", "sharedApplication");
            appDelegate = AppDelegateDefinition.CreateInstance(this);

            ObjC.Call(Handle, "setActivationPolicy:", IntPtr.Zero);
            ObjC.Call(Handle, "setDelegate:", appDelegate.Handle);

            ObjC.SetProperty(AppKit.GetClass("NSWindow"), "allowsAutomaticWindowTabbing", false);
        }

        public void Run()
        {
            ObjC.Call(Handle, "run");
        }

        public void Exit()
        {
            ObjC.Call(Handle, "terminate:", Handle);
            appDelegate.Dispose();
        }

        public void ApplyTheme(ApplicationTheme theme)
        {
            Appearance = theme switch
            {
                ApplicationTheme.Light => MacOsAppearance.VibrantLight,
                ApplicationTheme.Dark => MacOsAppearance.VibrantDark,
                _ => null,
            };
        }

        private static NativeClassDefinition CreateAppDelegate()
        {
            var definition = NativeClassDefinition.FromClass(
                "SpiderEyeAppDelegate",
                AppKit.GetClass("NSResponder"),
                // note: NSApplicationDelegate is not available at runtime and returns null
                AppKit.GetProtocol("NSApplicationDelegate"),
                AppKit.GetProtocol("NSTouchBarProvider"));

            definition.AddMethod<ShouldTerminateDelegate>(
                "applicationShouldTerminateAfterLastWindowClosed:",
                "c@:@",
                (self, op, notification) => (byte)(Application.ExitWithLastWindow ? 1 : 0));

            definition.AddMethod<NotificationDelegate>(
                "applicationDidFinishLaunching:",
                "v@:@",
                (self, op, notification) =>
                {
                    var instance = definition.GetParent<CocoaApplication>(self);
                    ObjC.Call(instance.Handle, "activateIgnoringOtherApps:", true);
                });

            definition.FinishDeclaration();

            return definition;
        }
    }
}
