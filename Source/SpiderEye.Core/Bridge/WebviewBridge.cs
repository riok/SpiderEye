using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SpiderEye.Bridge.Api;
using SpiderEye.Bridge.ClientServicesSupport;
using SpiderEye.Bridge.Models;

namespace SpiderEye.Bridge
{
    internal class WebviewBridge : IWebviewBridge
    {
        public event EventHandler<string> TitleChanged;
        public event EventHandler<string> MissingClientImplementationDetected;

        private IWebview Webview
        {
            get { return window.NativeWindow.Webview; }
        }

        private static event EventHandler<object> GlobalEventHandlerUpdate;
        private static event EventHandler<Type> GlobalTypeEventHandlerUpdate;
        private static readonly object GlobalHandlerLock = new object();
        private static readonly List<object> GlobalHandler = new List<object>();
        private static readonly List<Type> GlobalTypeHandler = new List<Type>();

        private static readonly IJsonConverter JsonConverter = new JsonNetJsonConverter();

        private readonly HashSet<string> apiRootNames = new HashSet<string>();
        private readonly Dictionary<string, ApiMethod> apiMethods = new Dictionary<string, ApiMethod>();

        private readonly Window window;
        private readonly IServiceProvider serviceProvider;

        private int callbackId;

        private readonly ConcurrentDictionary<int, (TaskCompletionSource<EventResultModel> TaskCompletionSource, BridgeClientMethodMissingMethodBehavior MissingBehavior)> eventCallbacks = new();

        public WebviewBridge(Window window, IServiceProvider serviceProvider = null)
        {
            this.window = window ?? throw new ArgumentNullException(nameof(window));
            this.serviceProvider = serviceProvider;

            InitApi();
        }

        public bool IsDependencyInjectionEnabled => serviceProvider != null;

        public static void AddGlobalHandler(object handler)
        {
            lock (GlobalHandlerLock)
            {
                GlobalHandler.Add(handler);
                GlobalEventHandlerUpdate?.Invoke(null, handler);
            }
        }

        public static void AddGlobalHandler<T>()
        {
            var type = typeof(T);
            lock (GlobalHandlerLock)
            {
                GlobalTypeHandler.Add(type);
                GlobalTypeEventHandlerUpdate?.Invoke(null, type);
            }
        }

        public void AddHandler(object handler)
        {
            AddApiObject(handler);
        }

        public void AddHandler<T>()
        {
            AddApiObject(typeof(T));
        }

        public void AddOrReplaceHandler(object handler)
        {
            AddApiObject(handler, true);
        }

        public void AddOrReplaceHandler<T>()
        {
            AddApiObject(typeof(T), true);
        }

        Task IWebviewBridge.InvokeAsync(string id, object data, BridgeClientMethodMissingMethodBehavior methodMissingMethodBehavior = BridgeClientMethodMissingMethodBehavior.Report)
            => InvokeAsync(id, data, methodMissingMethodBehavior);

        public async Task<EventResultModel> InvokeAsync(string id, object data, BridgeClientMethodMissingMethodBehavior methodMissingMethodBehavior = BridgeClientMethodMissingMethodBehavior.Report)
        {
            var callId = Interlocked.Increment(ref callbackId);
            string script = GetInvokeScript(id, callId, data);
            var completionSource = new TaskCompletionSource<EventResultModel>();
            if (!eventCallbacks.TryAdd(callId, (completionSource, methodMissingMethodBehavior)))
            {
                throw new InvalidOperationException("Callback id was already added...");
            }

            await Application.InvokeAsync(() => Webview.ExecuteScriptAsync(script));
            return await completionSource.Task;
        }

        public async Task<T> InvokeAsync<T>(string id, object data, BridgeClientMethodMissingMethodBehavior methodMissingMethodBehavior = BridgeClientMethodMissingMethodBehavior.Report)
        {
            var result = await InvokeAsync(id, data, methodMissingMethodBehavior);
            return ResolveInvokeResult<T>(result);
        }

        public async Task<object> InvokeAsync(string id, object data, Type returnType, BridgeClientMethodMissingMethodBehavior methodMissingMethodBehavior = BridgeClientMethodMissingMethodBehavior.Report)
        {
            var result = await InvokeAsync(id, data, methodMissingMethodBehavior);
            return ResolveInvokeResult(result, returnType);
        }

        public async Task HandleScriptCall(string data)
        {
            // run script call handling on separate task to free up UI
            await Task.Run(async () =>
            {
                var info = JsonConverter.Deserialize<InvokeInfoModel>(data);
                if (info == null)
                {
                    return;
                }

                try
                {
                    if (info.Type == "title")
                    {
                        string title = JsonConverter.Deserialize<string>(info.Parameters);
                        TitleChanged?.Invoke(this, title);
                    }
                    else if (info.Type == "api")
                    {
                        var result = await ResolveCall(info.Id, info.Parameters);
                        await EndApiCall(info, result);
                    }
                    else if (info.Type == "eventCallback" && info.CallbackId.HasValue)
                    {
                        if (!eventCallbacks.TryRemove(info.CallbackId.Value, out var callbackInfo))
                        {
                            Application.ReportInternalError($"No callback for eventCallBack with name {info.Id} and id {info.CallbackId}");
                            return;
                        }

                        var result = ResolveEventResult(info.Id, info.Parameters, callbackInfo.MissingBehavior);
                        callbackInfo.TaskCompletionSource.SetResult(result);
                    }
                    else if (info.CallbackId != null)
                    {
                        string message = $"Invalid invoke type \"{info.Type ?? "<null>"}\".";
                        await EndApiCall(info, ApiResultModel.FromError(message));
                    }
                }
                catch (Exception ex)
                {
                    // Not handling this exception would result in the whole application crashing.
                    // Report the error via other means, since we cannot throw an exception here.
                    Application.ReportInternalError($"Exception while handling script call for '{data}'", ex);
                }
            });
        }

        private string GetInvokeScript(string id, int callbackId, object data)
        {
            if (string.IsNullOrWhiteSpace(id)) { throw new ArgumentNullException(nameof(id)); }

            string dataJson = JsonConverter.Serialize(data);
            string idJson = JsonConverter.Serialize(id); // this makes sure that the name is properly escaped
            return $"window._spidereye._sendEvent({idJson}, {callbackId}, {dataJson})";
        }

        private EventResultModel ResolveEventResult(string id, string resultJson, BridgeClientMethodMissingMethodBehavior missingMethodBehavior)
        {
            var result = JsonConverter.Deserialize<EventResultModel>(resultJson);

            if (result.NoSubscriber)
            {
                switch (missingMethodBehavior)
                {
                    case BridgeClientMethodMissingMethodBehavior.Report:
                        MissingClientImplementationDetected?.Invoke(this, id);
                        return result;
                    case BridgeClientMethodMissingMethodBehavior.Ignore:
                        return result;
                    case BridgeClientMethodMissingMethodBehavior.Throw:
                        throw new MissingClientMethodImplementationException(id);
                }
            }

            if (result.Success)
            {
                return result;
            }

            string message = result.Error.Message;
            if (string.IsNullOrWhiteSpace(message)) { message = $"Error executing Event with ID \"{id}\"."; }
            else if (!string.IsNullOrWhiteSpace(result.Error.Name)) { message = $"{result.Error.Name}: {message}"; }

            string stackTrace = result.Error.Stack;
            if (string.IsNullOrWhiteSpace(stackTrace)) { throw new ScriptException(message); }

            throw new ScriptException(message, new ScriptException(message, stackTrace));
        }

        private object ResolveInvokeResult(EventResultModel result, Type t)
        {
            if (!result.HasResult) { return default; }
            else { return JsonConverter.Deserialize(result.Result, t); }
        }

        private T ResolveInvokeResult<T>(EventResultModel result)
        {
            if (!result.HasResult) { return default; }
            else { return JsonConverter.Deserialize<T>(result.Result); }
        }

        private async Task EndApiCall(InvokeInfoModel info, ApiResultModel result)
        {
            string resultJson = JsonConverter.Serialize(result);
            string script = $"window._spidereye._endApiCall({info.CallbackId}, {resultJson})";
            await Application.InvokeAsync(() => Webview.ExecuteScriptAsync(script));
        }

        private async Task<ApiResultModel> ResolveCall(string id, string parameters)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ApiResultModel.FromError("No API name given.");
            }

            if (apiMethods.TryGetValue(id, out ApiMethod info))
            {
                try
                {
                    object parametersObject = null;
                    if (info.HasParameter && !string.IsNullOrWhiteSpace(parameters))
                    {
                        parametersObject = JsonConverter.Deserialize(parameters, info.ParameterType);
                    }

                    object result = info is InstanceApiMethod instanceApiMethod
                        ? await instanceApiMethod.InvokeAsync(parametersObject)
                        : await InvokeWithDependencyInjection((IDependencyInjectionApiMethod)info, parametersObject);

                    return new ApiResultModel
                    {
                        Success = true,
                        Value = info.HasReturnValue ? result : null,
                    };
                }
                catch (TargetInvocationException tex) { return Application.ErrorMapper.MapErrorToApiResult(tex.InnerException ?? tex); }
                catch (Exception ex) { return Application.ErrorMapper.MapErrorToApiResult(ex); }
            }
            else { return ApiResultModel.FromError($"Unknown API call \"{id}\"."); }
        }

        private async Task<object> InvokeWithDependencyInjection(IDependencyInjectionApiMethod apiMethod, object parameters)
        {
            var scope = serviceProvider.CreateScope();
            await using var disposableWrapper = new AsyncDisposableWrapper(scope);
            return await apiMethod.InvokeAsync(scope.ServiceProvider, parameters);
        }

        private void AddApiObject(object handler, bool replaceIfExisting = false)
        {
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            Type type = handler.GetType();
            AddApiMethods(type, replaceIfExisting, method => new InstanceApiMethod(handler, method));
        }

        private void AddApiObject(Type type, bool replaceIfExisting = false)
        {
            if (!IsDependencyInjectionEnabled) { throw new InvalidOperationException("Cannot add handlers via type if dependency injection isn't enabled"); }

            AddApiMethods(type, replaceIfExisting, method => new DependencyInjectionApiMethod(type, method));
        }

        private void AddApiMethods(Type type, bool replaceIfExisting, Func<MethodInfo, ApiMethod> apiMethodFunc)
        {
            string rootName = type.Name;
            var attribute = type.GetCustomAttribute<BridgeObjectAttribute>();
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Path)) { rootName = attribute.Path; }

            if (!apiRootNames.Add(rootName) && !replaceIfExisting)
            {
                throw new InvalidOperationException($"Handler with name \"{rootName}\" already exists.");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.GetParameters().Length > 1) { continue; }

                var info = apiMethodFunc(method);
                string fullName = $"{rootName}.{info.Name}";

                if (!replaceIfExisting && apiMethods.ContainsKey(fullName))
                {
                    throw new InvalidOperationException($"Method with name \"{fullName}\" already exists.");
                }

                apiMethods[fullName] = info;
            }
        }

        private void InitApi()
        {
            AddApiObject(new WindowApiBridge(window, serviceProvider));
            AddApiObject(new DialogApiBridge(window));

            lock (GlobalHandlerLock)
            {
                GlobalEventHandlerUpdate += (s, e) => AddApiObject(e);
                GlobalTypeEventHandlerUpdate += (s, e) => AddApiObject(e);

                foreach (object handler in GlobalHandler)
                {
                    AddApiObject(handler);
                }

                foreach (var type in GlobalTypeHandler)
                {
                    AddApiObject(type);
                }
            }
        }
    }
}
