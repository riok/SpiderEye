using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SpiderEye.Bridge;
using SpiderEye.Linux.Interop;
using SpiderEye.Linux.Native;
using SpiderEye.Tools;

namespace SpiderEye.Linux
{
    internal class GtkWebview : IWebview
    {
        public event NavigatingEventHandler Navigating;

        public event EventHandler CloseRequested;
        public event EventHandler<string> TitleChanged;

        public bool EnableScriptInterface { get; set; }
        public bool UseBrowserTitle { get; set; }
        public bool EnableDevTools
        {
            get { return enableDevToolsField; }
            set
            {
                enableDevToolsField = value;
                WebKit.Settings.SetEnableDeveloperExtras(settings, true);
                if (value && loadEventHandled) { ShowDevTools(); }
                else if (!value) { CloseDevTools(); }
            }
        }

        public readonly IntPtr Handle;

        private const string CustomScheme = "spidereye";
        private const string DirectoryMappingPrefix = $"/{CustomScheme}-directory-mapping-";
        private const int UriCallbackFileNotFound = 4;
        private const int UriCallbackUnspecifiedError = 24;
        private readonly WebviewBridge bridge;
        private readonly IntPtr manager;
        private readonly IntPtr settings;
        private readonly IntPtr inspector;
        private readonly Uri customHost;

        private readonly ScriptDelegate scriptDelegate;
        private readonly PageLoadFailedDelegate loadFailedDelegate;
        private readonly PageLoadDelegate loadDelegate;
        private readonly ContextMenuRequestDelegate contextMenuDelegate;
        private readonly WebviewDelegate closeDelegate;
        private readonly WebviewDelegate titleChangeDelegate;
        private readonly WebKitUriSchemeRequestDelegate uriSchemeCallback;
        private readonly Dictionary<string, string> schemeToLocalDirectoryMapping = new();
        private readonly GAsyncReadyDelegate scriptExecuteCallback;

        private bool loadEventHandled = false;
        private bool enableDevToolsField;

        public GtkWebview(WebviewBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            // need to keep the delegates around or they will get garbage collected
            scriptDelegate = ScriptCallback;
            loadFailedDelegate = LoadFailedCallback;
            loadDelegate = LoadCallback;
            contextMenuDelegate = ContextMenuCallback;
            closeDelegate = CloseCallback;
            titleChangeDelegate = TitleChangeCallback;
            uriSchemeCallback = UriSchemeCallback;
            scriptExecuteCallback = ScriptExecuteCallback;

            manager = WebKit.Manager.Create();
            GLib.ConnectSignal(manager, "script-message-received::external", scriptDelegate, IntPtr.Zero);

            using (GLibString name = "external")
            {
                WebKit.Manager.RegisterScriptMessageHandler(manager, name);
            }

            using (GLibString initScript = Resources.GetInitScript("Linux"))
            {
                var script = WebKit.Manager.CreateScript(initScript, WebKitInjectedFrames.TopFrame, WebKitInjectionTime.DocumentStart, IntPtr.Zero, IntPtr.Zero);
                WebKit.Manager.AddScript(manager, script);
                WebKit.Manager.UnrefScript(script);
            }

            Handle = WebKit.CreateWithUserContentManager(manager);
            settings = WebKit.Settings.Get(Handle);
            inspector = WebKit.Inspector.Get(Handle);

            GLib.ConnectSignal(Handle, "load-failed", loadFailedDelegate, IntPtr.Zero);
            GLib.ConnectSignal(Handle, "load-changed", loadDelegate, IntPtr.Zero);
            GLib.ConnectSignal(Handle, "context-menu", contextMenuDelegate, IntPtr.Zero);
            GLib.ConnectSignal(Handle, "close", closeDelegate, IntPtr.Zero);
            GLib.ConnectSignal(Handle, "notify::title", titleChangeDelegate, IntPtr.Zero);

            customHost = UriTools.GetRandomResourceUrl(CustomScheme);

            IntPtr context = WebKit.Context.Get(Handle);
            using (GLibString gscheme = CustomScheme)
            {
                WebKit.Context.RegisterUriScheme(context, gscheme, uriSchemeCallback, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public Uri RegisterLocalDirectoryMapping(string directory)
        {
            // While GTK WebView allows to register custom schemes for this scenario, we ran into CORS errors since the scheme and host differ.
            // Trying to work around the CORS issues didn't work, so we chose a different approach where we re-use our existing custom scheme.
            // We just register a custom API path. When we receive a callback on that path, return the file contents.
            var customUrlPath = DirectoryMappingPrefix + schemeToLocalDirectoryMapping.Count;
            schemeToLocalDirectoryMapping.Add(customUrlPath, directory);
            return new Uri($"{customHost}{customUrlPath}/");
        }

        public void UpdateBackgroundColor(string color)
        {
            var bgColor = new GdkColor(color);
            WebKit.SetBackgroundColor(Handle, ref bgColor);
        }

        public void ShowDevTools()
        {
            WebKit.Inspector.Show(inspector);
        }

        public void CloseDevTools()
        {
            WebKit.Inspector.Close(inspector);
        }

        public void LoadUri(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            if (!uri.IsAbsoluteUri) { uri = new Uri(customHost, uri); }

            using (GLibString gurl = uri.ToString())
            {
                WebKit.LoadUri(Handle, gurl);
            }
        }

        public Task<string> ExecuteScriptAsync(string script)
        {
            var taskResult = new TaskCompletionSource<string>();
            var data = new ScriptExecuteState(taskResult);
            var handle = GCHandle.Alloc(data, GCHandleType.Normal);

            using (GLibString gfunction = script)
            {
                WebKit.JavaScript.BeginExecute(Handle, gfunction, IntPtr.Zero, scriptExecuteCallback, GCHandle.ToIntPtr(handle));
            }

            return taskResult.Task;
        }

        private unsafe void ScriptExecuteCallback(IntPtr webview, IntPtr asyncResult, IntPtr userdata)
        {
            var handle = GCHandle.FromIntPtr(userdata);
            var state = (ScriptExecuteState)handle.Target!;

            IntPtr jsResult = IntPtr.Zero;
            try
            {
                jsResult = WebKit.JavaScript.EndExecute(webview, asyncResult, out IntPtr errorPtr);
                if (jsResult != IntPtr.Zero)
                {
                    IntPtr value = WebKit.JavaScript.GetValue(jsResult);
                    if (WebKit.JavaScript.IsValueString(value))
                    {
                        IntPtr bytes = WebKit.JavaScript.GetStringBytes(value);
                        IntPtr bytesPtr = GLib.GetBytesDataPointer(bytes, out UIntPtr length);

                        string result = Encoding.UTF8.GetString((byte*)bytesPtr, (int)length);
                        state.TaskResult.TrySetResult(result);

                        GLib.UnrefBytes(bytes);
                    }
                    else { state.TaskResult.TrySetResult(null); }
                }
                else
                {
                    try
                    {
                        var error = Marshal.PtrToStructure<GError>(errorPtr);
                        string errorMessage = GLibString.FromPointer(error.Message);
                        state.TaskResult.TrySetException(new ScriptException($"Script execution failed with: (Code: {error.Code}, Msg: \"{errorMessage}\")"));
                    }
                    catch (Exception ex) { state.TaskResult.TrySetException(ex); }
                    finally { GLib.FreeError(errorPtr); }
                }
            }
            catch (Exception ex) { state.TaskResult.TrySetException(ex); }
            finally
            {
                handle.Free();

                if (jsResult != IntPtr.Zero)
                {
                    WebKit.JavaScript.ReleaseJsResult(jsResult);
                }
            }
        }

        public void Dispose()
        {
            // gets automatically disposed by parent window
        }

        private async void ScriptCallback(IntPtr manager, IntPtr jsResult, IntPtr userdata)
        {
            if (EnableScriptInterface)
            {
                IntPtr value = WebKit.JavaScript.GetValue(jsResult);
                if (WebKit.JavaScript.IsValueString(value))
                {
                    IntPtr bytes = IntPtr.Zero;
                    try
                    {
                        bytes = WebKit.JavaScript.GetStringBytes(value);
                        IntPtr bytesPtr = GLib.GetBytesDataPointer(bytes, out UIntPtr length);

                        string result;
                        unsafe { result = Encoding.UTF8.GetString((byte*)bytesPtr, (int)length); }

                        await bridge.HandleScriptCall(result);
                    }
                    finally { if (bytes != IntPtr.Zero) { GLib.UnrefBytes(bytes); } }
                }
            }
        }

        private async void UriSchemeCallback(IntPtr request, IntPtr userdata)
        {
            try
            {
                var uri = new Uri(GLibString.FromPointer(WebKit.UriScheme.GetRequestUri(request)));
                var scheme = uri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped);
                if (scheme != CustomScheme)
                {

                    FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                    return;
                }

                var path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                if (path.StartsWith(DirectoryMappingPrefix))
                {
                    foreach (var (pathPrefix, directory) in schemeToLocalDirectoryMapping)
                    {
                        if (path.StartsWith(pathPrefix))
                        {
                            // This is a request to a local file
                            LocalFileSchemeCallback(request, pathPrefix, directory);
                            return;
                        }
                    }
                }

                using var contentStream = await Application.ContentProvider.GetStreamAsync(uri);
                if (contentStream == null)
                {
                    FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                    return;
                }

                IntPtr stream = IntPtr.Zero;
                try
                {
                    if (contentStream is UnmanagedMemoryStream unmanagedMemoryStream)
                    {
                        unsafe
                        {
                            long length = unmanagedMemoryStream.Length - unmanagedMemoryStream.Position;
                            stream = GLib.CreateStreamFromData((IntPtr)unmanagedMemoryStream.PositionPointer, length, IntPtr.Zero);
                            FinishUriSchemeCallback(request, stream, length, uri);
                            return;
                        }
                    }
                    else
                    {
                        byte[] data;
                        long length;
                        if (contentStream is MemoryStream memoryStream)
                        {
                            data = memoryStream.GetBuffer();
                            length = memoryStream.Length;
                        }
                        else
                        {
                            using (var copyStream = new MemoryStream())
                            {
                                await contentStream.CopyToAsync(copyStream);
                                data = copyStream.GetBuffer();
                                length = copyStream.Length;
                            }
                        }

                        unsafe
                        {
                            fixed (byte* dataPtr = data)
                            {
                                stream = GLib.CreateStreamFromData((IntPtr)dataPtr, length, IntPtr.Zero);
                                FinishUriSchemeCallback(request, stream, length, uri);
                                return;
                            }
                        }
                    }
                }
                finally { if (stream != IntPtr.Zero) { GLib.UnrefObject(stream); } }
            }
            catch { FinishUriSchemeCallbackWithError(request, UriCallbackUnspecifiedError); }
        }

        private bool LoadFailedCallback(IntPtr webview, WebKitLoadEvent type, IntPtr failingUrl, IntPtr error, IntPtr userdata)
        {
            // this event is called when there is an error, immediately afterwards the LoadCallback is called with state Finished.
            // to indicate that there was an error and the PageLoaded event has been invoked, the loadEventHandled variable is set to true.
            loadEventHandled = true;
            return false;
        }

        private void LoadCallback(IntPtr webview, WebKitLoadEvent type, IntPtr userdata)
        {
            if (type == WebKitLoadEvent.Started)
            {
                loadEventHandled = false;
            }

            // this callback gets called in this order:
            // Started: initially defined URL
            // Redirected (optional, multiple): new URL to which the redirect points
            // Committed: final URL that gets loaded, either initial URL or same as last redirect URL
            // Finished: same URL as committed, page has fully loaded
            if (type == WebKitLoadEvent.Started || type == WebKitLoadEvent.Redirected)
            {
                string url = GLibString.FromPointer(WebKit.GetCurrentUri(webview));
                var args = new NavigatingEventArgs(new Uri(url));
                Navigating?.Invoke(this, args);
                if (args.Cancel) { WebKit.StopLoading(webview); }
            }
            else if (type == WebKitLoadEvent.Finished && !loadEventHandled)
            {
                if (EnableDevTools) { ShowDevTools(); }

                loadEventHandled = true;
            }
        }

        private void LocalFileSchemeCallback(IntPtr request, string pathPrefix, string directory)
        {
            var file = IntPtr.Zero;
            var fileStream = IntPtr.Zero;
            var fileInfo = IntPtr.Zero;

            try
            {
                var uri = new Uri(GLibString.FromPointer(WebKit.UriScheme.GetRequestUri(request)));
                var requestedFile = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)
                    .Substring(pathPrefix.Length)
                    .TrimStart('/');

                var fullFilePath = Path.GetFullPath(Path.Join(directory, requestedFile));
                if (!fullFilePath.StartsWith(directory))
                {
                    FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                    return;
                }

                using GLibString filePath = fullFilePath;
                file = GLib.FileForPath(filePath);
                fileStream = GLib.ReadFile(file, IntPtr.Zero, IntPtr.Zero);

                if (fileStream == IntPtr.Zero)
                {
                    FinishUriSchemeCallbackWithError(request, UriCallbackUnspecifiedError);
                    return;
                }

                using GLibString sizeAttribute = "standard::size";
                fileInfo = GLib.QueryFileInfo(fileStream, sizeAttribute, IntPtr.Zero, IntPtr.Zero);
                var fileSize = GLib.GetSizeFromFileInfo(fileInfo);
                FinishUriSchemeCallback(request, fileStream, fileSize, uri);
            }
            finally
            {
                if (file != IntPtr.Zero)
                {
                    GLib.UnrefObject(file);
                }

                if (fileStream != IntPtr.Zero)
                {
                    GLib.UnrefObject(fileStream);
                }

                if (fileInfo != IntPtr.Zero)
                {
                    GLib.UnrefObject(fileInfo);
                }
            }
        }

        private bool ContextMenuCallback(IntPtr webview, IntPtr default_menu, IntPtr hit_test_result, bool triggered_with_keyboard, IntPtr arg)
        {
            // this simply prevents the default context menu from showing up
            return true;
        }

        private void CloseCallback(IntPtr webview, IntPtr userdata)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TitleChangeCallback(IntPtr webview, IntPtr userdata)
        {
            string title = GLibString.FromPointer(WebKit.GetTitle(webview));
            TitleChanged?.Invoke(this, title);
        }

        private void FinishUriSchemeCallback(IntPtr request, IntPtr stream, long streamLength, Uri uri)
        {
            using (GLibString mimetype = Application.ContentProvider.GetMimeType(uri))
            {
                WebKit.UriScheme.FinishSchemeRequest(request, stream, streamLength, mimetype);
            }
        }

        private void FinishUriSchemeCallbackWithError(IntPtr request, int errorCode)
        {
            uint domain = GLib.GetFileErrorQuark();
            var error = new GError(domain, errorCode, IntPtr.Zero);
            WebKit.UriScheme.FinishSchemeRequestWithError(request, ref error);
        }

        private sealed class ScriptExecuteState
        {
            public readonly TaskCompletionSource<string> TaskResult;

            public ScriptExecuteState(TaskCompletionSource<string> taskResult)
            {
                TaskResult = taskResult;
            }
        }
    }
}
