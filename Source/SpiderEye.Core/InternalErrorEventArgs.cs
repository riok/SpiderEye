using System;

namespace SpiderEye;

public sealed class InternalErrorEventArgs : EventArgs
{
    public Exception Exception { get; set; }

    public string Message { get; set; }
}
