using System;
using System.Threading.Tasks;
using SpiderEye.Mac.Interop;
using SpiderEye.Mac.Native;

namespace SpiderEye.Mac;

public class CocoaClipboard : IClipboard
{
    private readonly IntPtr handle;
    private readonly IntPtr nsStringPboardType;

    public CocoaClipboard()
    {
        handle = NSPasteboard.Get();
        nsStringPboardType = NSString.Create("public.utf8-plain-text");
    }

    public Task<string> GetText()
    {
        var ptr = ObjC.Call(handle, "stringForType:", nsStringPboardType);
        var text = ptr == IntPtr.Zero
            ? string.Empty
            : NSString.GetString(ptr);
        return Task.FromResult(text);
    }

    public Task SetText(string text)
    {
        ObjC.Call(handle, "clearContents");
        var str = NSString.Create(text);
        ObjC.Call(handle, "setString:forType:", str, nsStringPboardType);
        return Task.CompletedTask;
    }
}
