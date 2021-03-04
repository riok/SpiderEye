using System;

namespace SpiderEye.Bridge.ClientServicesSupport
{
    /// <summary>
    /// Attribute to set specifics of the bridged client service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BridgeClientServiceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BridgeClientServiceAttribute"/> class.
        /// </summary>
        /// <param name="name">The name to be used for the client service. Defaults to the name of the type.</param>
        public BridgeClientServiceAttribute(string name = null)
        {
            Name = name;
        }

        /// <summary>
        /// The name prefix of the client service (a dot and the method name is appended).
        /// </summary>
        public string Name { get; }
    }
}
