namespace SpiderEye.Bridge.ClientServicesSupport
{
    public enum BridgeClientMethodMissingMethodBehavior
    {
        /// <summary>
        /// Reports the missing method as <see cref="IWebviewBridge.MissingClientImplementationDetected"/>.
        /// </summary>
        Report,

        /// <summary>
        /// Ignores the missing method, do not report anything.
        /// </summary>
        Ignore,

        /// <summary>
        /// Throw a <see cref="MissingClientMethodImplementationException"/> if the client method is missing.
        /// </summary>
        Throw,
    }
}
