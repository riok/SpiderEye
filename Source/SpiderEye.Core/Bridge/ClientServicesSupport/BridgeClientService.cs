using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SpiderEye.Bridge.ClientServicesSupport
{
    /// <summary>
    /// A ClientService represents a service which is implemented by the client.
    /// </summary>
    /// <typeparam name="T">Type of the Interface.</typeparam>
    public class BridgeClientService<T> : DispatchProxy
    {
        private static readonly MethodInfo InvokeMethod = typeof(IWebviewBridge)
            .GetMethods()
            .Single(m => m.Name == nameof(IWebviewBridge.InvokeAsync)
                         && m.IsGenericMethod
                         && m.GetGenericArguments().Length == 1
                         && m.GetParameters().Length == 3);

        private readonly string namePrefix = (typeof(T).GetCustomAttribute<BridgeClientServiceAttribute>()?.Name ?? typeof(T).Name) + ".";
        private WindowCollection windowCollection;

        /// <summary>
        /// Creates a proxy instance to the bridged client service.
        /// </summary>
        /// <param name="windowCollection">The window collection.</param>
        /// <returns>The created proxy instance.</returns>
        public static T Create(WindowCollection windowCollection)
        {
            var proxy = Create<T, BridgeClientService<T>>();
            var clientService = (BridgeClientService<T>)(object)proxy;
            clientService!.Init(windowCollection);
            return proxy;
        }

        /// <inheritdoc />
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var arg = args?.FirstOrDefault();
            var clientMethodAttribute = GetClientMethodAttribute(targetMethod);
            var callMode = GetCallMode(clientMethodAttribute);
            var missingMethodBehavior = GetMissingMethodBehavior(clientMethodAttribute);
            string callId = namePrefix + (clientMethodAttribute?.Name ?? targetMethod.Name);
            var resultType = targetMethod.ReturnType;
            if (resultType == typeof(Task))
            {
                resultType = typeof(void);
            }
            else if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                resultType = resultType.GetGenericArguments()[0];
            }

            switch (callMode)
            {
                case BridgeClientServiceCallMode.Broadcast:
                {
                    object result = null;
                    foreach (var win in windowCollection)
                    {
                        result = Invoke(resultType, win.Bridge, callId, arg, missingMethodBehavior);
                    }

                    return result;
                }

                case BridgeClientServiceCallMode.MainWindow:
                {
                    return Invoke(resultType, windowCollection.MainWindow.Bridge, callId, arg, missingMethodBehavior);
                }

                case BridgeClientServiceCallMode.SingleWindow:
                {
                    var window = args?.OfType<Window>().LastOrDefault()
                        ?? throw new ArgumentException($"if the call mode is {callMode}, the last provided argument must be the target window.");
                    return Invoke(resultType, window.Bridge, callId, arg, missingMethodBehavior);
                }

                default:
                    throw new ArgumentOutOfRangeException($"{callMode} is not a valid CallMode.");
            }
        }

        private object Invoke(
            Type resultType,
            IWebviewBridge bridge,
            string callId,
            object arg,
            BridgeClientMethodMissingMethodBehavior missingMethodBehavior)
        {
            if (resultType == typeof(void))
            {
                return bridge.InvokeAsync(callId, arg, missingMethodBehavior);
            }

            return InvokeMethod.MakeGenericMethod(resultType)
                .Invoke(bridge, [callId, arg, missingMethodBehavior]);
        }

        private void Init(WindowCollection winCollection)
        {
            windowCollection = winCollection;

            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException($"{typeof(T).FullName} must be an Interface");
            }

            foreach (var method in typeof(T).GetMethods())
            {
                CheckNumberOfParams(method);
            }
        }

        private void CheckNumberOfParams(MethodInfo method)
        {
            var clientMethodAttribute = GetClientMethodAttribute(method);
            var callMode = GetCallMode(clientMethodAttribute);
            int expectedMinParams = callMode == BridgeClientServiceCallMode.SingleWindow ? 1 : 0;
            int expectedMaxParams = expectedMinParams + 1;
            int paramsCount = method.GetParameters().Length;

            if (paramsCount > expectedMaxParams || paramsCount < expectedMinParams)
            {
                throw new InvalidOperationException(
                    $"the method {method.Name} of the client bound interface {typeof(T).FullName} is invalid. " +
                    $"For the call mode {callMode} only between {expectedMinParams} and {expectedMaxParams} parameters are allowed. " +
                    $"If a call mode expects a window, the last parameter should be the window.");
            }
        }

        private BridgeClientServiceCallMode GetCallMode(BridgeClientMethodAttribute clientMethodAttribute)
        {
            return clientMethodAttribute?.CallMode ?? BridgeClientServiceCallMode.MainWindow;
        }

        private BridgeClientMethodMissingMethodBehavior GetMissingMethodBehavior(BridgeClientMethodAttribute clientMethodAttribute)
        {
            return clientMethodAttribute?.MissingMethodBehavior ?? BridgeClientMethodMissingMethodBehavior.Report;
        }

        private BridgeClientMethodAttribute GetClientMethodAttribute(MethodInfo method)
        {
            return method.GetCustomAttribute<BridgeClientMethodAttribute>();
        }
    }
}
