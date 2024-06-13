using System;

namespace SpiderEye.Bridge.ClientServicesSupport;

/// <summary>
/// A client method that was invoked isn't (yet) registered in the webview.
/// </summary>
public class MissingClientMethodImplementationException : Exception
{
    public MissingClientMethodImplementationException(string id)
        : base($"Client service with id \"{id}\" does not exist.")
    {
    }
}
