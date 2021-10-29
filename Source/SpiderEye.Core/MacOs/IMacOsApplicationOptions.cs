namespace SpiderEye
{
    /// <summary>
    /// Accessor for mac os related application options.
    /// </summary>
    public interface IMacOsApplicationOptions
    {
        /// <summary>
        /// Gets or sets the mac os appearance.
        /// </summary>
        MacOsAppearance? Appearance { get; set; }

        /// <summary>
        /// Gets the effective mac os appearance.
        /// </summary>
        MacOsAppearance EffectiveAppearance { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to use a transparent title bar.
        /// </summary>
        bool TransparentTitleBar { get; set; }
    }
}
