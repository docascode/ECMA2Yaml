namespace CatLibrary
{
    /// <summary>
    /// It's the class that contains ICat interface's extension method.
    /// <para>This class must be <b>public</b> and <b>static</b>.</para>
    /// <para>Also it shouldn't be a geneic class</para>
    /// </summary>
    public static class ICatExtension
    {
        /// <summary>
        /// Extension method hint that how long the cat can sleep.
        /// </summary>
        /// <param name="icat">The type will be extended.</param>
        /// <param name="hours">The length of sleep.</param>
        public static void Sleep(this ICat<string, string> icat, long hours) { }

        /// <summary>
        /// Extension method to let cat play
        /// </summary>
        /// <param name="icat">Cat</param>
        /// <param name="toy">Something to play</param>
        public static void Play(this ICat<string, string> icat, string toy) { }
    }
}
