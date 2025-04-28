using System;

namespace SpiderEye;

public class OpenFileRequestEventArgs : CancelableEventArgs
{
    internal OpenFileRequestEventArgs(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
