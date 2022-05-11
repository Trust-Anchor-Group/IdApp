namespace IdApp.Services.Navigation
{
    /// <summary>
    /// An abstract base class holding page specific navigation parameters.
    /// </summary>
    public class NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NavigationArgs"/> class.
        /// </summary>
        public NavigationArgs() { }

        /// <summary>
        /// The route to use when going back, if any.
        /// </summary>
        public string ReturnRoute { get; set; }

        /// <summary>
        /// The number of times to pop when going back, if any.
        /// If this parrameter exist, it supercedes the <see cref="ReturnRoute"/>
        /// </summary>
        public int ReturnCount { get; set; }
    }
}