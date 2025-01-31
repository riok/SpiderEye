using System.Threading;

namespace SpiderEye
{
    /// <summary>
    /// Provides methods to manage and  run an application.
    /// </summary>
    internal interface IApplication
    {
        /// <summary>
        /// Gets the UI factory.
        /// </summary>
        IUiFactory Factory { get; }

        /// <summary>
        /// Gets the synchronization context.
        /// </summary>
        SynchronizationContext SynchronizationContext { get; }

        /// <summary>
        /// Gets the native options.
        /// </summary>
        object NativeOptions => null;

        /// <summary>
        /// Gets whether the dark mode is currently enabled.
        /// This returns true if the dark theme has been set or then the theme is the OS default and a dark theme is enabled for the whole OS.
        /// Returns null if the implementation does not support finding out whether dark mode is enabled.
        /// </summary>
        bool? IsDarkModeEnabled { get; }

        /// <summary>
        /// Starts the main loop and blocks until the application exits.
        /// </summary>
        void Run();

        /// <summary>
        /// Exits the main loop and allows it to return.
        /// </summary>
        void Exit();

        /// <summary>
        /// Applies a theme to the whole application through native APIs.
        /// Note that you probably need to set a background color in addition to this to make it look good.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        void ApplyTheme(ApplicationTheme theme);
    }
}
