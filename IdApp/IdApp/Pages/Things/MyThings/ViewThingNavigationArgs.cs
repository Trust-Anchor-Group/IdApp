using IdApp.Services;
using IdApp.Services.Navigation;
using System.Threading.Tasks;

namespace IdApp.Pages.Things.MyThings
{
    /// <summary>
    /// Holds navigation parameters specific to viewing things.
    /// </summary>
    public class MyThingsNavigationArgs : NavigationArgs
    {
        private readonly TaskCompletionSource<ContactInfo> thingToShare;

        /// <summary>
        /// Creates a new instance of the <see cref="MyThingsNavigationArgs"/> class.
        /// </summary>
        /// <param name="ThingToShare">Task completion source where the selected thing is placed.</param>
        public MyThingsNavigationArgs(TaskCompletionSource<ContactInfo> ThingToShare)
        {
            this.thingToShare = ThingToShare;
        }

        /// <summary>
        /// Task completion source where the selected thing is placed.
        /// </summary>
        public TaskCompletionSource<ContactInfo> ThingToShare => this.thingToShare;
    }
}