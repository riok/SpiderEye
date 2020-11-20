using System;
using System.Threading.Tasks;

namespace SpiderEye.Bridge
{
    /// <summary>
    /// An interface to circumvent the generic types on the implemented class.
    /// </summary>
    internal interface IDependencyInjectionApiMethod
    {
        /// <summary>
        /// Invoke the api method.
        /// </summary>
        /// <param name="serviceProvider">The service provider, from which the api instance will be resolved.</param>
        /// <param name="parameter">The api method parameters.</param>
        /// <returns>The api method result.</returns>
        Task<object> InvokeAsync(IServiceProvider serviceProvider, object parameter);
    }
}
