namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Provides access to local preferences.
    /// </summary>
    public static class LocalPrefs
    {
        /// <summary>
        /// Shared instance of <see cref="ILocalPrefs"/>.
        /// </summary>
        public static ILocalPrefs Shared { get; internal set; }
    }
}