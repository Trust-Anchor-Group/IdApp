namespace IdApp.Services.Navigation
{
    /// <summary>
    /// An abstract base class holding page specific navigation parameters.
    /// </summary>
    public abstract class NavigationArgs
    {
        /// <summary>
        /// The route to use when going back, if any.
        /// </summary>
        public string ReturnRoute { get; set; }
    }
}