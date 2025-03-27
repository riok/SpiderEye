namespace SpiderEye
{
    /// <summary>
    /// Accessor for mac os related application options.
    /// </summary>
    public interface ILinuxApplicationOptions
    {
        /// <summary>
        /// Gets or sets the linux application id.
        /// </summary>
        string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the application flags.
        /// </summary>
        LinuxApplicationFlags ApplicationFlags { get; set; }
    }
}
