using System;
using System.Threading.Tasks;

namespace SpiderEye.Bridge
{
    /// <summary>
    /// Represents a bridge between the host and the webview.
    /// </summary>
    public interface IWebviewBridge
    {
        /// <summary>
        /// Event that is emitted whenever a missing client implementation was detected.
        /// </summary>
        event EventHandler<string> MissingClientImplementationDetected;

        /// <summary>
        /// Gets a value indicating whether dependency injection is enabled for the bridge.
        /// If this is true, bridge calls run in a scoped DI lifetime.
        /// </summary>
        public bool IsDependencyInjectionEnabled { get; }

        /// <summary>
        /// Adds a custom handler to be called from the webview.
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        void AddHandler(object handler);

        /// <summary>
        /// Adds a custom handler to be called from the webview.
        /// Note: This method throws if <see cref="IsDependencyInjectionEnabled"/> is false.
        /// </summary>
        /// <typeparam name="T">The handler type.</typeparam>
        void AddHandler<T>();

        /// <summary>
        /// Adds or replaces a custom handler to be called from the webview.
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        void AddOrReplaceHandler(object handler);

        /// <summary>
        /// Adds or replaces a custom handler to be called from the webview.
        /// </summary>
        /// <typeparam name="T">The handler type.</typeparam>
        void AddOrReplaceHandler<T>();

        /// <summary>
        /// Asynchronously invokes an event in the webview.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="data">Optional event data.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task InvokeAsync(string id, object data);

        /// <summary>
        /// Asynchronously invokes an event in the webview and get the result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="id">The event ID.</param>
        /// <param name="data">Optional event data.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<T> InvokeAsync<T>(string id, object data);

        /// <summary>
        /// Asynchronously invokes an event in the webview and get the result.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="data">Optional event data.</param>
        /// <param name="returnType">The result type.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<object> InvokeAsync(string id, object data, Type returnType);
    }
}
