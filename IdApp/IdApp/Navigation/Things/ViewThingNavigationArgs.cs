using Tag.Neuron.Xamarin.Services;

namespace IdApp.Navigation.Things
{
    /// <summary>
    /// Holds navigation parameters specific to viewing things.
    /// </summary>
    public class ViewThingNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ViewThingNavigationArgs"/> class.
        /// </summary>
        /// <param name="Thing">Thing information.</param>
        public ViewThingNavigationArgs(ContactInfo Thing)
        {
            this.Thing = Thing;
        }

        /// <summary>
        /// Thing information.
        /// </summary>
        public ContactInfo Thing { get; }
    }
}