using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Gdk;
using Gio.Internal;
using GObject;
using SpiderEye.Bridge;
using SpiderEye.Tools;
using WebKit;
using Bytes = GLib.Bytes;
using Error = GLib.Error;
using Functions = Gio.Functions;
using InputStream = Gio.InputStream;
using MemoryInputStream = Gio.MemoryInputStream;
using Object = GObject.Object;
using Settings = WebKit.Settings;
using Uri = System.Uri;

namespace SpiderEye.Linux
{
    [UnsupportedOSPlatform("OSX")]
    [UnsupportedOSPlatform("Windows")]
    internal class GtkWebview : IWebview
    {
        public event NavigatingEventHandler Navigating;

        public event EventHandler CloseRequested;
        public event EventHandler<string> TitleChanged;

        public bool EnableScriptInterface { get; set; }
        public bool UseBrowserTitle { get; set; }
        public bool EnableDevTools
        {
            get { return settings.EnableDeveloperExtras; }
            set
            {
                settings.SetEnableDeveloperExtras(value);
                if (value && loadEventHandled) { ShowDevTools(); }
                else if (!value) { CloseDevTools(); }
            }
        }

        public WebView WebView { get; }

        private const string CustomScheme = "spidereye";
        private const string DirectoryMappingPrefix = $"/{CustomScheme}-directory-mapping-";
        private const int UriCallbackFileNotFound = 4;
        private const int UriCallbackUnspecifiedError = 24;
        private static readonly Dictionary<string, string> SchemeToLocalDirectoryMapping = new();
        private static readonly Uri CustomHost;
        private readonly WebviewBridge bridge;
        private readonly UserContentManager manager;
        private readonly Settings settings;
        private readonly WebInspector inspector;

        private bool loadEventHandled;

        static GtkWebview()
        {
            CustomHost = UriTools.GetRandomResourceUrl(CustomScheme);

            var context = WebContext.GetDefault();
            context.RegisterUriScheme(CustomScheme, UriSchemeCallback);
        }

        public GtkWebview(WebviewBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            WebView = WebView.New();
            manager = WebView.GetUserContentManager();
            manager.OnScriptMessageReceived += ScriptCallback;
            manager.RegisterScriptMessageHandler("external", null);
            using var script = UserScript.New(
                Resources.GetInitScript("Linux"),
                UserContentInjectedFrames.TopFrame,
                UserScriptInjectionTime.Start,
                null,
                null);
            manager.AddScript(script);

            settings = WebView.GetSettings();
            inspector = WebView.GetInspector();

            WebView.OnLoadFailed += LoadFailedCallback;
            WebView.OnLoadChanged += LoadCallback;
            WebView.OnContextMenu += ContextMenuCallback;
            WebView.OnClose += CloseCallback;

            Object.NotifySignal.Connect(
                WebView,
                TitleChangeCallback,
                detail: "title");
        }

        public Uri RegisterLocalDirectoryMapping(string directory)
        {
            // While GTK WebView allows to register custom schemes for this scenario, we ran into CORS errors since the scheme and host differ.
            // Trying to work around the CORS issues didn't work, so we chose a different approach where we re-use our existing custom scheme.
            // We just register a custom API path. When we receive a callback on that path, return the file contents.
            var customUrlPath = DirectoryMappingPrefix + SchemeToLocalDirectoryMapping.Count;
            SchemeToLocalDirectoryMapping.Add(customUrlPath, directory);
            return new Uri($"{CustomHost}{customUrlPath}/");
        }

        public void UpdateBackgroundColor(string color)
        {
            using var rgba = new RGBA();
            rgba.Parse(color);
            WebView.SetBackgroundColor(rgba);
        }

        public void ShowDevTools()
        {
            inspector.Show();
        }

        public void CloseDevTools()
        {
            inspector.Close();
        }

        public void LoadUri(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            if (!uri.IsAbsoluteUri) { uri = new Uri(CustomHost, uri); }

            WebView.LoadUri(uri.ToString());
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            using var result = await WebView.EvaluateJavascriptAsync(script);
            return result.IsString()
                ? result.ToString()
                : null;
        }

        public void Dispose()
        {
            // gets automatically disposed by parent window
        }

        private async void ScriptCallback(UserContentManager m, UserContentManager.ScriptMessageReceivedSignalArgs args)
        {
            if (EnableScriptInterface && args.Value.IsString())
            {
                await bridge.HandleScriptCall(args.Value.ToString());
            }
        }

        private static async void UriSchemeCallback(URISchemeRequest request)
        {
            try
            {
                var uri = new Uri(request.GetUri());
                var scheme = uri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped);
                if (scheme != CustomScheme)
                {
                    FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                    return;
                }

                var path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
                if (path.StartsWith(DirectoryMappingPrefix))
                {
                    foreach (var (pathPrefix, directory) in SchemeToLocalDirectoryMapping)
                    {
                        if (path.StartsWith(pathPrefix))
                        {
                            // This is a request to a local file
                            LocalFileSchemeCallback(request, pathPrefix, directory);
                            return;
                        }
                    }
                }

                await using var contentStream = await Application.ContentProvider.GetStreamAsync(uri);
                if (contentStream == null)
                {
                    FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                    return;
                }

                if (contentStream is UnmanagedMemoryStream unmanagedMemoryStream)
                {
                    unsafe
                    {
                        long length = unmanagedMemoryStream.Length - unmanagedMemoryStream.Position;
                        IntPtr handle = Gio.Internal.MemoryInputStream.NewFromData(ref *unmanagedMemoryStream.PositionPointer, (IntPtr)length, null);
                        using var stream = new MemoryInputStream(new MemoryInputStreamHandle(handle, true));
                        FinishUriSchemeCallback(request, stream, length, uri);
                    }

                    return;
                }

                byte[] data;
                long streamLength;
                if (contentStream is MemoryStream memoryStream)
                {
                    data = memoryStream.GetBuffer();
                    streamLength = memoryStream.Length;
                }
                else
                {
                    using var copyStream = new MemoryStream();
                    await contentStream.CopyToAsync(copyStream);
                    data = copyStream.GetBuffer();
                    streamLength = copyStream.Length;
                }

                using var bytes = Bytes.New(data);
                using var ms = MemoryInputStream.NewFromBytes(bytes);
                FinishUriSchemeCallback(request, ms, streamLength, uri);
            }
            catch { FinishUriSchemeCallbackWithError(request, UriCallbackUnspecifiedError); }
        }

        private bool LoadFailedCallback(WebView webview, WebView.LoadFailedSignalArgs args)
        {
            // this event is called when there is an error, immediately afterwards the LoadCallback is called with state Finished.
            // to indicate that there was an error and the PageLoaded event has been invoked, the loadEventHandled variable is set to true.
            loadEventHandled = true;
            return false;
        }

        private void LoadCallback(WebView webview, WebView.LoadChangedSignalArgs signalArgs)
        {
            var type = signalArgs.LoadEvent;
            if (type == LoadEvent.Started)
            {
                loadEventHandled = false;
            }

            // this callback gets called in this order:
            // Started: initially defined URL
            // Redirected (optional, multiple): new URL to which the redirect points
            // Committed: final URL that gets loaded, either initial URL or same as last redirect URL
            // Finished: same URL as committed, page has fully loaded
            if (type == LoadEvent.Started || type == LoadEvent.Redirected)
            {
                var args = new NavigatingEventArgs(new Uri(WebView.GetUri()));
                Navigating?.Invoke(this, args);
                if (args.Cancel) { WebView.StopLoading(); }
            }
            else if (type == LoadEvent.Finished && !loadEventHandled)
            {
                if (EnableDevTools) { ShowDevTools(); }

                loadEventHandled = true;
            }
        }

        private static void LocalFileSchemeCallback(URISchemeRequest request, string pathPrefix, string directory)
        {
            var uri = new Uri(request.GetUri());
            var requestedFile = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)
                .Substring(pathPrefix.Length)
                .TrimStart('/');

            var fullFilePath = Path.GetFullPath(Path.Join(directory, requestedFile));
            if (!fullFilePath.StartsWith(directory))
            {
                FinishUriSchemeCallbackWithError(request, UriCallbackFileNotFound);
                return;
            }

            using var file = Functions.FileNewForPath(fullFilePath);
            using var fileStream = file.Read(null);

            var fileInfo = fileStream.QueryInfo("standard::size", null);
            var fileSize = fileInfo.GetSize();

            FinishUriSchemeCallback(request, fileStream, fileSize, uri);
        }

        private static void FinishUriSchemeCallbackWithError(URISchemeRequest request, int errorCode)
        {
            using var error = new Error();
            error.Code = errorCode;
            request.FinishError(error);
        }

        private bool ContextMenuCallback(WebView w, WebView.ContextMenuSignalArgs args)
        {
            // this simply prevents the default context menu from showing up
            return true;
        }

        private void CloseCallback(WebView w, EventArgs args)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void TitleChangeCallback(Object o, SignalArgs args)
        {
            TitleChanged?.Invoke(this, WebView.GetTitle());
        }

        private static void FinishUriSchemeCallback(URISchemeRequest request, InputStream stream, long streamLength, Uri uri)
        {
            request.Finish(stream, streamLength, Application.ContentProvider.GetMimeType(uri));
        }
    }
}
