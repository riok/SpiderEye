using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SpiderEye.Bridge.ClientServicesSupport
{
    /// <summary>
    /// A ClientService represents a service which is implemented by the client.
    /// </summary>
    /// <typeparam name="T">Type of the Interface.</typeparam>
    public class BridgeClientService<T> : DispatchProxy
    {
        private static readonly MethodInfo _awaitGenericTaskMethod = typeof(BridgeClientService<T>).GetMethod(nameof(AwaitGenericTask), BindingFlags.NonPublic | BindingFlags.Static)
                                                                     ?? throw new InvalidOperationException("Could not find await task method");
        private static readonly MethodInfo _awaitTaskMethod = typeof(BridgeClientService<T>).GetMethod(nameof(AwaitTask), BindingFlags.NonPublic | BindingFlags.Static)
                                                                     ?? throw new InvalidOperationException("Could not find await task method");

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
            var resultType = targetMethod.ReturnType.IsGenericType && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                ? targetMethod.ReturnType.GetGenericTypeDefinition().GetGenericArguments()[0]
                : targetMethod.ReturnType;

            object result = null;
            switch (callMode)
            {
                case BridgeClientServiceCallMode.Broadcast:
                {
                    foreach (var win in windowCollection)
                    {
                        result = win.Bridge.InvokeAsync(callId, arg, resultType, missingMethodBehavior);
                    }

                    break;
                }

                case BridgeClientServiceCallMode.MainWindow:
                {
                    result = windowCollection.MainWindow.Bridge.InvokeAsync(callId, arg, resultType, missingMethodBehavior);
                    break;
                }

                case BridgeClientServiceCallMode.SingleWindow:
                {
                    var window = args?.OfType<Window>().LastOrDefault()
                        ?? throw new ArgumentException($"if the call mode is {callMode}, the last provided argument must be the target window.");
                    result = window.Bridge.InvokeAsync(callId, arg, resultType, missingMethodBehavior);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException($"{callMode} is not a valid CallMode.");
            }

            if (result is not Task task)
            {
                return result;
            }

            var actualResultType = result.GetType();
            if (actualResultType == typeof(Task))
            {
                return AwaitTask(task);
            }

            if (actualResultType.IsGenericType &&
                actualResultType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return _awaitGenericTaskMethod
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new object[] { task });
            }

            throw new InvalidOperationException("Unsupported return type.");
        }

        private static async Task AwaitTask(Task task)
        {
            await task;
        }

        private static async Task<TResult> AwaitGenericTask<TResult>(Task task)
        {
            return await (Task<TResult>)task;
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
