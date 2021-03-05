namespace SpiderEye.Bridge.ClientServicesSupport
{
    public enum BridgeClientServiceCallMode
    {
        /// <summary>
        /// The invoke call targets only the first opened window.
        /// </summary>
        MainWindow,

        /// <summary>
        /// The invoke call targets only a single window.
        /// </summary>
        SingleWindow,

        /// <summary>
        /// The invoke call targets all windows.
        /// The last result is returned.
        /// </summary>
        Broadcast,
    }
}
