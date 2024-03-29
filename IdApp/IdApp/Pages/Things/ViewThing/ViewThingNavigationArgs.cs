﻿using IdApp.Services;
using IdApp.Services.Navigation;
using IdApp.Services.Notification;

namespace IdApp.Pages.Things.ViewThing
{
    /// <summary>
    /// Holds navigation parameters specific to viewing things.
    /// </summary>
    public class ViewThingNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ViewThingNavigationArgs"/> class.
        /// </summary>
        public ViewThingNavigationArgs() { }

        /// <summary>
        /// Creates a new instance of the <see cref="ViewThingNavigationArgs"/> class.
        /// </summary>
        /// <param name="Thing">Thing information.</param>
		/// <param name="Events">Notification events.</param>
        public ViewThingNavigationArgs(ContactInfo Thing, NotificationEvent[] Events)
        {
            this.Thing = Thing;
			this.Events = Events;
        }

        /// <summary>
        /// Thing information.
        /// </summary>
        public ContactInfo Thing { get; }

		/// <summary>
		/// Notification events.
		/// </summary>
		public NotificationEvent[] Events { get; }

	}
}
