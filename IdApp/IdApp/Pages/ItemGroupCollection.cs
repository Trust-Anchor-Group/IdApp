using System.Collections.ObjectModel;

namespace IdApp.Pages
{
	/// <summary>
	/// Grouped item interface.
	/// </summary>
	public interface IItemGroup
	{
		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName { get; }
	}

	/// <summary>
	/// Grouped item collection interface.
	/// </summary>
	public interface IItemGroupCollection
	{
		/// <summary>
		/// Unique name used to compare items.
		/// </summary>
		public string UniqueName { get; }
	}

	/// <summary>
	/// Encapsulates a grouped item collection.
	/// </summary>
	public class ItemGroupCollection<T> : ObservableCollection<T>, IItemGroupCollection where T : IItemGroup
	{
		/// <inheritdoc/>
		public string UniqueName { get; }

		/// <summary>
		/// Encapsulates a grouped item collection.
		/// </summary>
		/// <param name="Name">Group's unique name.</param>
		/// <param name="Items">Group's item collection.</param>
		public ItemGroupCollection(string Name, ObservableCollection<T> Items) : base(Items)
		{
			this.UniqueName = Name;
		}
	}
}
