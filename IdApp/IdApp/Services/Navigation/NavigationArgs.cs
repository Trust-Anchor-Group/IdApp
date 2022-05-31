namespace IdApp.Services.Navigation
{
    /// <summary>
    /// An base class holding page specific navigation parameters.
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
        /// Set it to 1 to start a counter of the number of times to pop when going back.
        /// It will be incremented on every push using the <see cref="INavigationService.GoToAsync"/> method.
        /// If this parrameter exist, it supercedes the <see cref="ReturnRoute"/>
        /// </summary>
        public int ReturnCounter { get; set; }

        /// <summary>
        /// Set it to true to start a new <see cref="ReturnCounter"/> session.
        /// If the ReturnCounter is zero, it will stop the further counting.
        /// </summary>
        public bool CancelReturnCounter { get; set; }

		/// <summary>
		/// An untique view identificator used to search the args of similar view types.
		/// </summary>
		public string UniqueId { get; set; }
	}
}
