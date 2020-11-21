using System;

namespace SpiderEye.Mac.Interop
{
    internal static class NSRunLoopMode
    {
        /// <summary>
        /// The mode to deal with input sources other than NSConnection objects.
        /// </summary>
        public static readonly IntPtr NSDefaultRunLoopMode = NSString.Create("NSDefaultRunLoopMode");
    }
}
