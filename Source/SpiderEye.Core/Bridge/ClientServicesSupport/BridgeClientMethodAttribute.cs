using System;

namespace SpiderEye.Bridge.ClientServicesSupport
{
    /// <summary>
    /// Sets a specific options for a bridged client method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class BridgeClientMethodAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BridgeClientMethodAttribute"/> class.
        /// </summary>
        /// <param name="callMode">The call mode.</param>
        /// <param name="name">The name of the method.</param>
        public BridgeClientMethodAttribute(BridgeClientServiceCallMode callMode = BridgeClientServiceCallMode.MainWindow, string name = null)
        {
            CallMode = callMode;
            Name = name;
        }

        /// <summary>
        /// Gets the call mode which should be used for this method.
        /// </summary>
        public BridgeClientServiceCallMode CallMode { get; }

        /// <summary>
        /// Name of the method on the client (gets prefix with the class name separated by a dot).
        /// </summary>
        public string Name { get; }
    }
}
