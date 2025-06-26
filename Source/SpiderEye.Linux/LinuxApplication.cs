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
