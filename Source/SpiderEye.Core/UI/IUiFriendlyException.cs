using System.Diagnostics.CodeAnalysis;

namespace SpiderEye.UI
{
    /// <summary>
    /// Exceptions implementing this interface signal that they have a readable <see cref="UiMessage"/>, which can be shown in the UI (whatever that may be).
    /// </summary>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "This is exception related")]
    public interface IUiFriendlyException
    {
        /// <summary>
        /// Gets the readable exception message.
        /// </summary>
        public string UiMessage { get; }
    }
}
