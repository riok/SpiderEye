using Microsoft.Extensions.DependencyInjection.Extensions;
using SpiderEye;
using SpiderEye.Bridge.ClientServicesSupport;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class BridgeServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a ui bridge service via dependency injection as a scoped service (a scope is created for each request).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddSpidereyeBridgeScoped<T>(this IServiceCollection services) where T : class
        {
            services.TryAddScoped<T>();
            Application.AddGlobalHandler<T>();
            return services;
        }

        /// <summary>
        /// Registers a ui bridge service via dependency injection as a singleton service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddSpidereyeBridgeSingleton<T>(this IServiceCollection services) where T : class
        {
            services.TryAddSingleton<T>();
            Application.AddGlobalHandler<T>();
            return services;
        }

        /// <summary>
        /// Registers a ui client service via dependency injection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <typeparam name="T">The interface of the client methods.</typeparam>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddSpidereyeBridgeClientService<T>(this IServiceCollection services) where T : class
        {
            services.TryAddSingleton(_ => BridgeClientService<T>.Create(Application.OpenWindows));
            return services;
        }
    }
}
