namespace SpiderEye.Windows
{
    /// <summary>
    /// Provides Windows specific application methods.
    /// </summary>
    public static class WindowsApplication
    {
        internal static string ContentServerAddress
        {
            get { return app.ContentServer.HostAddress; }
        }

        private static WinFormsApplication app;

        /// <summary>
        /// Initializes the application.
        /// </summary>
        public static void Init()
        {
            app = new WinFormsApplication();
            Application.Register(app, OperatingSystem.Windows);
        }
    }
}
