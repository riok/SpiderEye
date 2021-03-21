using System;

namespace SpiderEye.Linux
{
    /// <summary>
    /// Provides Linux specific application methods.
    /// </summary>
    public static class LinuxApplication
    {
        private static GtkApplication app;

        /// <summary>
        /// Initializes the application.
        /// </summary>
        public static void Init()
        {
            try
            {
                app = new GtkApplication();
            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine("Dependencies are missing. Please make sure that 'libgtk-3' and 'libwebkit2gtk-4.0' are installed.");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(-1);
            }

            Application.Register(app, OperatingSystem.Linux);
        }
    }
}
