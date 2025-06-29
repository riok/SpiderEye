using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SpiderEye.Bridge;
#if !NET462
using System.Runtime.InteropServices;
#endif

namespace SpiderEye
{
    /// <summary>
    /// Provides methods to create or run an application.
    /// </summary>
    public static class Application
    {
        /// <summary>
        /// Event handler to subscribe to SpiderEye internal errors that cannot be caught via normal exception handling.
        /// </summary>
        public static event EventHandler<InternalErrorEventArgs> InternalError;

        /// <summary>
        /// Event handler to subscribe to whenever a file should be opened with the application.
        /// Currently only supported on macos.
        /// </summary>
        public static event EventHandler<OpenFileRequestEventArgs> OpenFileRequested;

        /// <summary>
        /// Gets or sets a value indicating whether the application should exit once the last window is closed.
        /// Default is true.
        /// </summary>
        public static bool ExitWithLastWindow { get; set; }

        /// <summary>
        /// Gets a collection of windows that are currently open.
        /// </summary>
        public static WindowCollection OpenWindows { get; }

        /// <summary>
        /// Gets mac os related window options.
        /// </summary>
        public static IMacOsApplicationOptions MacOsOptions => app.NativeOptions as IMacOsApplicationOptions;

        /// <summary>
        /// Gets linux related window options.
        /// </summary>
        public static ILinuxApplicationOptions LinuxOptions => app.NativeOptions as ILinuxApplicationOptions;

        /// <summary>
        /// Gets the OS clipboard.
        /// </summary>
        public static IClipboard Clipboard => app.Clipboard;

        /// <summary>
        /// Gets whether the dark mode is currently enabled.
        /// This returns true if the dark theme has been set or then the theme is the OS default and a dark theme is enabled for the whole OS.
        /// Returns null if the implementation does not support finding out whether dark mode is enabled.
        /// </summary>
        public static bool? IsDarkModeEnabled => app.IsDarkModeEnabled;

        /// <summary>
        /// Gets or sets the content provider for loading webview files.
        /// </summary>
        public static IContentProvider ContentProvider
        {
            get { return contentProvider; }
            set { contentProvider = value ?? NoopContentProvider.Instance; }
        }

        /// <summary>
        /// Gets or sets the URI watcher to check URIs before they are loaded.
        /// </summary>
        public static IUriWatcher UriWatcher
        {
            get { return uriWatcher; }
            set { uriWatcher = value ?? NoopUriWatcher.Instance; }
        }

        /// <summary>
        /// Gets the operating system the app is currently running on.
        /// </summary>
        public static OperatingSystem OS { get; }

        /// <summary>
        /// Gets or sets the error mapper.
        /// </summary>
        public static ErrorMapper ErrorMapper { get; set; }

        /// <summary>
        /// Gets or sets the storage to save window information (size, position).
        /// </summary>
        public static IWindowStorage WindowInfoStorage { get; set; }

        /// <summary>
        /// Gets the UI factory.
        /// </summary>
        internal static IUiFactory Factory
        {
            get
            {
                CheckInitialized();
                return app.Factory;
            }
        }

        private static IApplication app;
        private static IContentProvider contentProvider;
        private static IUriWatcher uriWatcher;

        static Application()
        {
            OS = GetOS();
            ExitWithLastWindow = true;
            contentProvider = NoopContentProvider.Instance;
            uriWatcher = NoopUriWatcher.Instance;
            OpenWindows = new WindowCollection();
            ErrorMapper = new ErrorMapper();
        }

        /// <summary>
        /// Adds a custom handler to be called from any webview of the application.
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        public static void AddGlobalHandler(object handler)
        {
            WebviewBridge.AddGlobalHandler(handler);
        }

        /// <summary>
        /// Adds a custom handler to be called from any webview of the application.
        /// </summary>
        /// <typeparam name="T">The handler type.</typeparam>
        public static void AddGlobalHandler<T>()
        {
            WebviewBridge.AddGlobalHandler<T>();
        }

        /// <summary>
        /// Adds a custom json converter.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <typeparam name="T">The type of the converted object.</typeparam>
        public static void AddJsonConverter<T>(JsonConverter<T> converter)
        {
            JsonNetJsonConverter.Settings.Converters.Add(converter);
        }

        /// <summary>
        /// Adds a custom json converter.
        /// </summary>
        /// <param name="converter">the converter.</param>
        public static void AddJsonConverter(JsonConverter converter)
        {
            JsonNetJsonConverter.Settings.Converters.Add(converter);
        }

        /// <summary>
        /// Starts the main loop and blocks until the application exits.
        /// </summary>
        public static void Run()
        {
            CheckInitialized();

            app.Run();
        }

        /// <summary>
        /// Starts the main loop, shows the given window and blocks until the application exits.
        /// </summary>
        /// <param name="window">The window to show.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is null.</exception>
        public static void Run(Window window)
        {
            if (window == null) { throw new ArgumentNullException(nameof(window)); }

            window.Show();
            Run();
        }

        /// <summary>
        /// Starts the main loop, loads the URL, shows the given window and blocks until the application exits.
        /// </summary>
        /// <param name="window">The window to show.</param>
        /// <param name="startUrl">The initial URL to load in the window.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="startUrl"/> is null.</exception>
        public static void Run(Window window, string startUrl)
        {
            if (window == null) { throw new ArgumentNullException(nameof(window)); }
            if (startUrl == null) { throw new ArgumentNullException(nameof(startUrl)); }

            window.LoadUrl(startUrl);
            window.Show();
            Run();
        }

        /// <summary>
        /// Exits the main loop and allows it to return.
        /// </summary>
        public static void Exit()
        {
            CheckInitialized();

            app.Exit();
        }

        /// <summary>
        /// Applies a theme to the whole application through native APIs.
        /// Note that you probably need to set a background color in addition to this to make it look good.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        public static void ApplyTheme(ApplicationTheme theme)
        {
            CheckInitialized();

            app.ApplyTheme(theme);
        }

        /// <summary>
        /// Executes the given action on the UI main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void Invoke(Action action)
        {
            if (action == null) { throw new ArgumentNullException(nameof(action)); }

            InvokeSafely(action);
        }

        /// <summary>
        /// Executes the given function on the UI main thread.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <returns>The result of the given function.</returns>
        public static T Invoke<T>(Func<T> function)
        {
            if (function == null) { throw new ArgumentNullException(nameof(function)); }

            T result = default;
            InvokeSafely(() => result = function());
            return result;
        }

        /// <summary>
        /// Executes the given task on the UI main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static async Task InvokeAsync(Func<Task> action)
        {
            if (action == null) { throw new ArgumentNullException(nameof(action)); }
            await InvokeSafelyAsync<object>(async () =>
            {
                await action();
                return null;
            });
        }

        /// <summary>
        /// Executes the given task on the UI main thread.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <returns>The result of the given function.</returns>
        public static Task<T> InvokeAsync<T>(Func<Task<T>> function)
        {
            if (function == null) { throw new ArgumentNullException(nameof(function)); }
            return InvokeSafelyAsync(function);
        }

        /// <summary>
        /// Checks if the current operating system is correct.
        /// </summary>
        /// <param name="application">The application OS specific implementation.</param>
        /// <param name="applicationOS">The operating system the implementation is made for.</param>
        internal static void Register(IApplication application, OperatingSystem applicationOS)
        {
            if (OS != applicationOS)
            {
                string msg = $"Wrong platform: using {applicationOS} specific library on {OS}";
                throw new PlatformNotSupportedException(msg);
            }

            app = application ?? throw new ArgumentNullException(nameof(application));

            SynchronizationContext.SetSynchronizationContext(app.SynchronizationContext);
        }

        internal static bool OpenFile(string filePath)
        {
            var args = new OpenFileRequestEventArgs(filePath);
            OpenFileRequested?.Invoke(null, args);
            return !args.Cancel;
        }

        internal static void ReportInternalError(string message, Exception exception = null)
        {
            var eventArgs = new InternalErrorEventArgs
            {
                Message = message,
                Exception = exception,
            };
            InternalError?.Invoke(typeof(Application), eventArgs);
        }

        private static void InvokeSafely(Action action)
        {
            CheckInitialized();

            ExceptionDispatchInfo exception = null;
            app.SynchronizationContext.Send(
                state =>
                {
                    try { action(); }
                    catch (Exception ex) { exception = ExceptionDispatchInfo.Capture(ex); }
                }, null);

            exception?.Throw();
        }

        private static Task<T> InvokeSafelyAsync<T>(Func<Task<T>> action)
        {
            CheckInitialized();

            var tcs = new TaskCompletionSource<T>();
            app.SynchronizationContext.Post(async _ =>
            {
                try
                {
                    var result = await action().ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        private static void CheckInitialized()
        {
            if (app == null)
            {
                string platform;
                switch (OS)
                {
                    case OperatingSystem.Windows:
                        platform = "Windows";
                        break;
                    case OperatingSystem.MacOS:
                        platform = "Mac";
                        break;
                    case OperatingSystem.Linux:
                        platform = "Linux";
                        break;

                    default:
                        throw new PlatformNotSupportedException();
                }

                string message = $"Application has not been initialized yet. Call {platform}Application.Init() first.";
                throw new InvalidOperationException(message);
            }
        }

        private static OperatingSystem GetOS()
        {
#if NET462
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    return OperatingSystem.Windows;
                case PlatformID.MacOSX:
                    return OperatingSystem.MacOS;
                case PlatformID.Unix:
                    return OperatingSystem.Linux;

                default:
                    throw new PlatformNotSupportedException();
            }
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return OperatingSystem.Windows; }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return OperatingSystem.MacOS; }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return OperatingSystem.Linux; }
            else { throw new PlatformNotSupportedException(); }
#endif
        }
    }
}
