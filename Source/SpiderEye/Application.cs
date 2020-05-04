using SpiderEye.Mac;

namespace SpiderEye
{
    public static class SpiderEyeApp
    {
        public static void Init()
        {
#if MacOs
            MacApplication.Init();
#elif Linux
            LinuxApplication.Init();
#else
            WindowsApplication.Init();
#endif
        }

        public static void Run(Window window, string startLocation)
        {
            Application.Run(window, startLocation);
        }
    }
}
