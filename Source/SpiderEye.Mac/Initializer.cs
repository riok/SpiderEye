using SpiderEye.Mac;

namespace SpiderEye.Native
{
    public static class SpiderEyeInitializer
    {
        public static void Init(string appId)
        {
            MacApplication.Init();
        }
    }
}
