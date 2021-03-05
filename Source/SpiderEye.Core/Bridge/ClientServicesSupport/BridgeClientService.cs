using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SpiderEye.Bridge.ClientServicesSupport
{
    /// <summary>
    /// A ClientService represents a service which is implemented by the client.
    /// </summary>
    /// <typeparam name="T">Type of the Interface.</typeparam>
    public class BridgeClientService<T> : DispatchProxy
    {
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
            var callMode = GetCallMode(targetMethod);
            string callId = namePrefix + (targetMethod.GetCustomAttribute<BridgeClientMethodAttribute>()?.Name ?? targetMethod.Name);

            switch (callMode)
            {
                case BridgeClientServiceCallMode.Broadcast:
                {
                    object result = null;
                    foreach (var win in windowCollection)
                    {
                        result = win.Bridge.InvokeAsync(callId, arg, targetMethod.ReturnType);
                    }

                    return result;
                }

                case BridgeClientServiceCallMode.MainWindow:
                {
                    return windowCollection.MainWindow.Bridge.InvokeAsync(callId, arg, targetMethod.ReturnType);
                }

                case BridgeClientServiceCallMode.SingleWindow:
                {
                    var window = args?.OfType<Window>().LastOrDefault()
                        ?? throw new ArgumentException($"if the call mode is {callMode}, the last provided argument must be the target window.");
                    return window.Bridge.InvokeAsync(callId, arg, targetMethod.ReturnType);
                }

                default:
                    throw new ArgumentOutOfRangeException($"{callMode} is not a valid CallMode.");
            }
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
            var callMode = GetCallMode(method);
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

        private BridgeClientServiceCallMode GetCallMode(MethodInfo method)
        {
            return method.GetCustomAttribute<BridgeClientMethodAttribute>()?.CallMode ?? BridgeClientServiceCallMode.MainWindow;
        }
    }
}
