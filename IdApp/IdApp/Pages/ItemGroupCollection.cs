using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.Pages
{
	/// <summary>
	/// Grouped item interface.
	/// </summary>
	public interface IUniqueItem
	{
		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName { get; }
	}

	/// <summary>
	/// Encapsulates a grouped item collection.
	/// </summary>
	public class ObservableItemGroup<T> : ObservableRangeCollection<T>, IUniqueItem where T : IUniqueItem
	{
		/// <inheritdoc/>
		public string UniqueName { get; }

		/// <summary>
		/// Encapsulates a grouped item collection.
		/// </summary>
		/// <param name="Name">Group's unique name.</param>
		/// <param name="Items">Group's item collection.</param>
		public ObservableItemGroup(string Name, List<T> Items) : base(Items)
		{
			this.UniqueName = Name;
		}

		/// <summary>
		/// Get the groups's name (potentially used for display)
		/// </summary>
		public override string ToString()
		{
			return this.UniqueName;
		}
	}
}
