using SpiderEye.Windows;

namespace SpiderEye.Native
{
    public static class SpiderEyeInitializer
    {
        public static void Init(string appId)
        {
            WindowsApplication.Init();
        }
    }
}
