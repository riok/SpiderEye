﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SpiderEye.Bridge;
using SpiderEye.Mac.Interop;
using SpiderEye.Mac.Native;
using SpiderEye.Tools;

namespace SpiderEye.Mac
{
    internal class CocoaWebview : IWebview
    {
        private const string SpiderEyeScheme = "spidereye";
        private const string DirectoryMappingPrefix = $"{SpiderEyeScheme}-directory-mapping-";

        public event NavigatingEventHandler Navigating;
        public event EventHandler<string> TitleChanged;

        public bool EnableScriptInterface { get; set; }
        public bool UseBrowserTitle { get; set; }
        public bool EnableDevTools
        {
            get { return enableDevToolsField; }
            set
            {
                enableDevToolsField = value;
                IntPtr boolValue = Foundation.Call("NSNumber", "numberWithBool:", value);
                ObjC.Call(preferences, "setValue:forKey:", boolValue, NSString.Create("developerExtrasEnabled"));
            }
        }

        private Uri Uri
        {
            get { return URL.GetAsUri(ObjC.Call(Handle, "URL")); }
        }

        public readonly IntPtr Handle;

        private static readonly NativeClassDefinition CallbackClassDefinition;
        private static readonly NativeClassDefinition SchemeHandlerDefinition;

        private readonly NativeClassInstance callbackClass;
        private readonly NativeClassInstance schemeHandler;

        private readonly WebviewBridge bridge;
        private readonly Uri customHost;
        private readonly Dictionary<string, string> urlPathDirectoryMappings = new Dictionary<string, string>();
        private readonly IntPtr preferences;

        private bool enableDevToolsField;

        static CocoaWebview()
        {
            CallbackClassDefinition = CreateCallbackClass();
            SchemeHandlerDefinition = CreateSchemeHandler();
        }

        public CocoaWebview(WebviewBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            IntPtr configuration = WebKit.Call("WKWebViewConfiguration", "new");
            IntPtr manager = ObjC.Call(configuration, "userContentController");

            callbackClass = CallbackClassDefinition.CreateInstance(this);
            schemeHandler = SchemeHandlerDefinition.CreateInstance(this);

            customHost = UriTools.GetRandomResourceUrl(SpiderEyeScheme);
            ObjC.Call(configuration, "setURLSchemeHandler:forURLScheme:", schemeHandler.Handle, NSString.Create(SpiderEyeScheme));

            ObjC.Call(manager, "addScriptMessageHandler:name:", callbackClass.Handle, NSString.Create("external"));
            IntPtr script = WebKit.Call("WKUserScript", "alloc");
            ObjC.Call(
                script,
                "initWithSource:injectionTime:forMainFrameOnly:",
                NSString.Create(Resources.GetInitScript("Mac")),
                IntPtr.Zero,
                IntPtr.Zero);
            ObjC.Call(manager, "addUserScript:", script);

            Handle = WebKit.Call("WKWebView", "alloc");
            ObjC.Call(Handle, "initWithFrame:configuration:", CGRect.Zero, configuration);
            ObjC.Call(Handle, "setNavigationDelegate:", callbackClass.Handle);

            IntPtr boolValue = Foundation.Call("NSNumber", "numberWithBool:", false);
            ObjC.Call(Handle, "setValue:forKey:", boolValue, NSString.Create("drawsBackground"));
            ObjC.Call(Handle, "addObserver:forKeyPath:options:context:", callbackClass.Handle, NSString.Create("title"), IntPtr.Zero, IntPtr.Zero);

            preferences = ObjC.Call(configuration, "preferences");
        }

        public void UpdateBackgroundColor(IntPtr color)
        {
            ObjC.Call(Handle, "setBackgroundColor:", color);
        }

        public void LoadUri(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            if (!uri.IsAbsoluteUri) { uri = new Uri(customHost, uri); }

            var uriStr = uri.ToString().Replace(" ", "%20");
            IntPtr nsUrl = Foundation.Call("NSURL", "URLWithString:", NSString.Create(uriStr));
            IntPtr request = Foundation.Call("NSURLRequest", "requestWithURL:", nsUrl);
            ObjC.Call(Handle, "loadRequest:", request);
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            var taskResult = new TaskCompletionSource<string>();

            ScriptEvalCallbackDelegate callback = (IntPtr self, IntPtr result, IntPtr error) =>
            {
                try
                {
                    if (error != IntPtr.Zero)
                    {
                        string message = NSString.GetString(ObjC.Call(error, "localizedDescription"));
                        taskResult.TrySetException(new Exception($"Script execution failed with: \"{message}\""));
                    }
                    else
                    {
                        string content = NSString.GetString(result);
                        taskResult.TrySetResult(content);
                    }
                }
                catch (Exception ex) { taskResult.TrySetException(ex); }
            };

            using var block = new NSBlock(callback);
            ObjC.Call(
                Handle,
                "evaluateJavaScript:completionHandler:",
                NSString.Create(script),
                block.Handle);
            return await taskResult.Task;
        }

        public Uri RegisterLocalDirectoryMapping(string directory)
        {
            // While WebView allows to register custom schemes for this scenario, we ran into CORS errors since the scheme and host differ.
            // Trying to work around the CORS issues didn't work, so we chose a different approach where we re-use our existing custom scheme.
            // We just register a custom API path. When we receive a callback on that path, return the file contents.
            // The count is used as an identifier, since directory mappings cannot be unregistered.
            var customUrlPath = DirectoryMappingPrefix + urlPathDirectoryMappings.Count;
            urlPathDirectoryMappings.Add(customUrlPath, Path.GetFullPath(directory));
            return new Uri($"{customHost}{customUrlPath}/");
        }

        public void Dispose()
        {
            // webview will be released automatically
            callbackClass.Dispose();
            schemeHandler.Dispose();
        }

        private (Stream Content, string MimeType)? GetContent(Uri uri)
        {
            var host = new Uri(uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));
            if (host != customHost)
            {
                return null;
            }

            string path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            if (path.StartsWith(DirectoryMappingPrefix)
                && GetMappedDirectoryContent(path) is { } content)
            {
                return content;
            }

            return (
                Application.ContentProvider.GetStreamAsync(uri).GetAwaiter().GetResult(),
                Application.ContentProvider.GetMimeType(uri));
        }

        private (Stream Content, string MimeType)? GetMappedDirectoryContent(string path)
        {
            bool found = false;
            foreach (var (pathPrefix, directory) in urlPathDirectoryMappings)
            {
                if (!path.StartsWith(pathPrefix))
                {
                    continue;
                }

                path = path[pathPrefix.Length..];
                path = Path.GetFullPath(Path.Join(directory, path));
                if (!path.StartsWith(directory))
                {
                    return null;
                }

                found = true;
                break;
            }

            if (!found)
            {
                return null;
            }

            return (File.OpenRead(path), MimeTypes.FindForFile(path));
        }

        private static NativeClassDefinition CreateCallbackClass()
        {
            var definition = NativeClassDefinition.FromObject(
                "SpiderEyeWebviewCallbacks",
                WebKit.GetProtocol("WKNavigationDelegate"),
                // note: WKScriptMessageHandler is not available at runtime and returns null
                WebKit.GetProtocol("WKScriptMessageHandler"));

            definition.AddMethod<NavigationDecideDelegate>(
                "webView:decidePolicyForNavigationAction:decisionHandler:",
                "v@:@@@",
                (self, op, view, navigationAction, decisionHandler) =>
                {
                    bool cancel = false;
                    var instance = definition.GetParent<CocoaWebview>(self);
                    if (instance != null)
                    {
                        var args = new NavigatingEventArgs(instance.Uri);
                        instance.Navigating?.Invoke(instance, args);
                        cancel = args.Cancel;
                    }


                    var block = Marshal.PtrToStructure<NSBlock.BlockLiteral>(decisionHandler);
                    var callback = Marshal.GetDelegateForFunctionPointer<NavigationDecisionDelegate>(block.Invoke);
                    callback(decisionHandler, cancel ? IntPtr.Zero : new IntPtr(1));
                });

            definition.AddMethod<ObserveValueDelegate>(
                "observeValueForKeyPath:ofObject:change:context:",
                "v@:@@@@",
                (self, op, keyPath, obj, change, context) =>
                {
                    var instance = definition.GetParent<CocoaWebview>(self);
                    if (instance != null)
                    {
                        ObservedValueChanged(instance, keyPath);
                    }
                });

            definition.AddMethod<ScriptCallbackDelegate>(
                "userContentController:didReceiveScriptMessage:",
                "v@:@@",
                (self, op, notification, message) =>
                {
                    var instance = definition.GetParent<CocoaWebview>(self);
                    if (instance != null)
                    {
                        ScriptCallback(instance, message);
                    }
                });

            definition.FinishDeclaration();

            return definition;
        }

        private static NativeClassDefinition CreateSchemeHandler()
        {
            var definition = NativeClassDefinition.FromObject(
                "SpiderEyeSchemeHandler",
                // note: WKURLSchemeHandler is not available at runtime and returns null
                WebKit.GetProtocol("WKURLSchemeHandler"));

            definition.AddMethod<SchemeHandlerDelegate>(
                "webView:startURLSchemeTask:",
                "v@:@@",
                (self, op, view, schemeTask) =>
                {
                    var instance = definition.GetParent<CocoaWebview>(self);
                    if (instance != null)
                    {
                        UriSchemeStartCallback(instance, schemeTask);
                    }
                });

            definition.AddMethod<SchemeHandlerDelegate>(
                "webView:stopURLSchemeTask:",
                "v@:@@",
                (self, op, view, schemeTask) => { /* don't think anything needs to be done here */ });

            definition.FinishDeclaration();

            return definition;
        }

        private static void ObservedValueChanged(CocoaWebview instance, IntPtr keyPath)
        {
            string key = NSString.GetString(keyPath);
            if (key == "title" && instance.UseBrowserTitle)
            {
                string title = NSString.GetString(ObjC.Call(instance.Handle, "title"));
                instance.TitleChanged?.Invoke(instance, title ?? string.Empty);
            }
        }

        private static async void ScriptCallback(CocoaWebview instance, IntPtr message)
        {
            if (instance.EnableScriptInterface)
            {
                IntPtr body = ObjC.Call(message, "body");
                IntPtr isString = ObjC.Call(body, "isKindOfClass:", Foundation.GetClass("NSString"));
                if (isString != IntPtr.Zero)
                {
                    string data = NSString.GetString(body);
                    await instance.bridge.HandleScriptCall(data);
                }
            }
        }

        private static void UriSchemeStartCallback(CocoaWebview instance, IntPtr schemeTask)
        {
            try
            {
                IntPtr request = ObjC.Call(schemeTask, "request");
                IntPtr url = ObjC.Call(request, "URL");

                var uri = new Uri(NSString.GetString(ObjC.Call(url, "absoluteString")));
                if (uri.Scheme != SpiderEyeScheme)
                {
                    FinishUriSchemeCallbackWithError(schemeTask, 404);
                    return;
                }

                var content = instance.GetContent(uri);
                if (!content.HasValue)
                {
                    FinishUriSchemeCallbackWithError(schemeTask, 404);
                    return;
                }

                using var contentStream = content.Value.Content;
                var mimeType = content.Value.MimeType;

                if (contentStream is UnmanagedMemoryStream unmanagedMemoryStream)
                {
                    unsafe
                    {
                        FinishUriSchemeCallback(
                            url,
                            schemeTask,
                            (IntPtr)unmanagedMemoryStream.PositionPointer,
                            unmanagedMemoryStream.Length - unmanagedMemoryStream.Position,
                            mimeType);
                        return;
                    }
                }

                byte[] data;
                long length;
                if (contentStream is MemoryStream memoryStream)
                {
                    data = memoryStream.GetBuffer();
                    length = memoryStream.Length;
                }
                else
                {
                    using var copyStream = new MemoryStream();
                    contentStream.CopyTo(copyStream);
                    data = copyStream.GetBuffer();
                    length = copyStream.Length;
                }

                unsafe
                {
                    fixed (byte* dataPtr = data)
                    {
                        FinishUriSchemeCallback(url, schemeTask, (IntPtr)dataPtr, length, mimeType);
                    }
                }
            }
            catch { FinishUriSchemeCallbackWithError(schemeTask, 500); }
        }

        private static void FinishUriSchemeCallback(
            IntPtr url,
            IntPtr schemeTask,
            IntPtr data,
            long contentLength,
            string mimeType)
        {
            IntPtr response = Foundation.Call("NSURLResponse", "alloc");
            ObjC.Call(
                response,
                "initWithURL:MIMEType:expectedContentLength:textEncodingName:",
                url,
                NSString.Create(mimeType),
                new IntPtr(contentLength),
                IntPtr.Zero);

            ObjC.Call(schemeTask, "didReceiveResponse:", response);

            IntPtr nsData = Foundation.Call(
                "NSData",
                "dataWithBytesNoCopy:length:freeWhenDone:",
                data,
                new IntPtr(contentLength),
                IntPtr.Zero);
            ObjC.Call(schemeTask, "didReceiveData:", nsData);

            ObjC.Call(schemeTask, "didFinish");
        }

        private static void FinishUriSchemeCallbackWithError(IntPtr schemeTask, int errorCode)
        {
            var error = Foundation.Call(
                "NSError",
                "errorWithDomain:code:userInfo:",
                NSString.Create("com.bildstein.spidereye"),
                new IntPtr(errorCode),
                IntPtr.Zero);
            ObjC.Call(schemeTask, "didFailWithError:", error);
        }
    }
}
