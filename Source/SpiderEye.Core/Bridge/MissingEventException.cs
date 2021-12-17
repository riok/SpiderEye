using System;

namespace SpiderEye.Bridge
{
    /// <summary>
    /// An event that was invoked isn't (yet) registered in the webview.
    /// </summary>
    public class MissingEventException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingEventException"/> class.
        /// </summary>
        /// <param name="id">The missing event id/name.</param>
        public MissingEventException(string id)
            : base($"Event with ID \"{id}\" does not exist.")
        {
        }
    }
}

