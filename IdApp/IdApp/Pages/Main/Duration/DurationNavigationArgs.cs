﻿using IdApp.Services.Navigation;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Duration
{
	/// <summary>
	/// Holds navigation parameters for the duration.
	/// </summary>
	public class DurationNavigationArgs : NavigationArgs
	{
		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		public DurationNavigationArgs()
			: this(null)
		{
		}

		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		/// <param name="Entry">Entry whose value is being calculated.</param>
		public DurationNavigationArgs(Entry Entry)
		{
			this.Entry = Entry;
			this.ViewModel = null;
			this.Property = null;
		}

		/// <summary>
		/// Holds navigation parameters for the duration.
		/// </summary>
		/// <param name="ViewModel">View model containing a bindable property with the value to calculate.</param>
		/// <param name="Property">Property containing the value to calculate.</param>
		public DurationNavigationArgs(BaseViewModel ViewModel, BindableProperty Property)
		{
			this.Entry = null;
			this.ViewModel = ViewModel;
			this.Property = Property;
		}

		/// <summary>
		/// Entry whose value is being calculated.
		/// </summary>
		public Entry Entry { get; }

		/// <summary>
		/// View model containing a bindable property with the value to calculate.
		/// </summary>
		public BaseViewModel ViewModel { get; }

		/// <summary>
		/// Property containing the value to calculate.
		/// </summary>
		public BindableProperty Property { get; }
	}
}
