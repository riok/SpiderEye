using System;
using System.IO;

namespace SpiderEye.Linux
{
    /// <summary>
    /// Provides Linux specific application methods.
    /// </summary>
    public static class LinuxApplication
    {
        internal static GtkApplication App { get; private set; }

        /// <summary>
        /// Initializes the application.
        /// </summary>
        public static void Init()
        {
            if (Environment.GetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER") == null)
            {
                // This helps with a lot of webkit bugs (black screen, wrong zoom level etc.)
                // Since this may impact performance, we only set this if it was not set explicitly by the user
                Environment.SetEnvironmentVariable("WEBKIT_DISABLE_DMABUF_RENDERER", "1");
            }

            try
            {
                App = new GtkApplication();
            }
            catch (Exception ex) when (ex is DllNotFoundException or FileNotFoundException)
            {
                Console.WriteLine("Dependencies are missing. Please make sure that 'libgtk-4-1' and 'libwebkitgtk-6.0-4' are installed.");
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
            }

            Application.Register(App, OperatingSystem.Linux);
        }
    }
}
