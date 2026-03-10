using SpiderEye.Linux;

namespace SpiderEye.Native
{
    public static class SpiderEyeInitializer
    {
        public static void Init(string appId)
        {
            LinuxApplication.Init(appId);
        }
    }
}
