using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SpiderEye.Bridge;
using SpiderEye.Tools;

namespace SpiderEye.Windows
{
    internal class WinFormsWebview : IWebview, IWinFormsWebview
    {
        public event NavigatingEventHandler Navigating;

        public Control Control
        {
            get { return webview; }
        }

        public bool EnableScriptInterface
        {
            get { return webview.CoreWebView2?.Settings?.IsWebMessageEnabled ?? initialWebMessageEnabled; }
            set
            {
                if (webview.CoreWebView2 == null)
                {
                    initialWebMessageEnabled = value;
                    return;
                }

                webview.CoreWebView2.Settings.IsWebMessageEnabled = value;
            }
        }

        // Note: We currently can't use a custom scheme, since the WebView2 doesn't support it yet
        private const string CustomScheme = "http";
        private const string WebView2UserDataFolder = "SpiderEyeWebView2";
        private readonly WebviewBridge bridge;
        private readonly Uri customHost;
        private WebView2 webview;
        private CoreWebView2Environment webView2Environment;
        private string initialUriToLoad;
        private bool initialWebMessageEnabled;

        public WinFormsWebview(WebviewBridge bridge)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

            webview = new WebView2();
            InitializeWebView();

            webview.WebMessageReceived += Webview_WebMessageReceived;

            webview.NavigationStarting += Webview_NavigationStarting;
            webview.CoreWebView2InitializationCompleted += WebViewInitializationCompleted;

            customHost = new Uri(UriTools.GetRandomResourceUrl(CustomScheme));
        }

        public void UpdateBackgroundColor(byte r, byte g, byte b)
        {
            webview.BackColor = Color.FromArgb(r, g, b);
        }

        public void LoadUri(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            if (!uri.IsAbsoluteUri) { uri = new Uri(customHost, uri); }

            if (webview.CoreWebView2 == null)
            {
                initialUriToLoad = uri.ToString();
                return;
            }

            webview.CoreWebView2.Navigate(uri.ToString());
        }

        public Task<string> ExecuteScriptAsync(string script)
        {
            return webview.ExecuteScriptAsync(script);
        }

        public void Dispose()
        {
            webview?.Dispose();
        }

        private async void InitializeWebView()
        {
            var tempFolder = Path.GetTempPath();
            var webView2UserDataFolder = Path.Combine(tempFolder, WebView2UserDataFolder);

            try
            {
                webView2Environment = await CoreWebView2Environment.CreateAsync(null, webView2UserDataFolder);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show("The WebView2 isn't installed. It is required for this app.");
                Environment.Exit(-1);
            }

            await webview.EnsureCoreWebView2Async(webView2Environment);
        }

        private async void WebViewInitializationCompleted(object sender, EventArgs e)
        {
            EnableScriptInterface = initialWebMessageEnabled;

            webview.CoreWebView2.WebResourceRequested += Webview_WebResourceRequested;
            webview.CoreWebView2.AddWebResourceRequestedFilter($"{customHost}*", CoreWebView2WebResourceContext.All);

            string initScript = Resources.GetInitScript("Windows");
            await webview.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(initScript);

            if (initialUriToLoad != null)
            {
                webview.CoreWebView2.Navigate(initialUriToLoad);
            }
        }

        private void Webview_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var requestUri = new Uri(e.Request.Uri);
            var host = new Uri(requestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped));

            if (host != customHost)
            {
                return;
            }

            try
            {
                var contentStream = Application.ContentProvider.GetStreamAsync(requestUri).GetAwaiter().GetResult();
                if (contentStream == null)
                {
                    e.Response = webView2Environment.CreateWebResourceResponse(null, 404, "Not Found", string.Empty);
                    return;
                }

                var mimeType = Application.ContentProvider.GetMimeType(requestUri);
                e.Response = webView2Environment.CreateWebResourceResponse(contentStream, 200, "OK", $"Content-Type: {mimeType}");
            }
            catch (Exception ex)
            {
                e.Response = webView2Environment.CreateWebResourceResponse(null, 500, ex.Message, string.Empty);
            }
        }

        private async void Webview_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            await bridge.HandleScriptCall(e.WebMessageAsJson);
        }

        private void Webview_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var args = new NavigatingEventArgs(new Uri(e.Uri));
            Navigating?.Invoke(this, args);
            e.Cancel = args.Cancel;
        }
    }
}
