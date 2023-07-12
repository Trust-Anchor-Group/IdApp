using System.Collections.Generic;
using System.Linq;
using Xamarin.CommunityToolkit.Helpers;
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

		/// <summary>
		/// Get the groups's localised name
		/// </summary>
		public string LocalisedName => this.UniqueName;
	}

	/// <summary>
	/// Encapsulates a grouped item collection.
	/// </summary>
	public class ObservableItemGroup<T> : ObservableRangeCollection<T>, IUniqueItem where T : IUniqueItem
	{
		/// <inheritdoc/>
		public string UniqueName { get; }

		/// <inheritdoc/>
		public string LocalisedName
		{
			get
			{
				return LocalizationResourceManager.Current[this.UniqueName] ?? this.UniqueName;
			}
		}

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

		/// <summary>
		/// Update the current collection items using a new collection
		/// </summary>
		public static void UpdateGroupsItems(ObservableItemGroup<IUniqueItem> OldCollection, ObservableItemGroup<IUniqueItem> NewCollection)
		{
			// First, remove items which are no longer in the new collection
			List<IUniqueItem> RemoveItems = OldCollection.Where(oel => NewCollection.All(nel => !nel.UniqueName.Equals(oel.UniqueName))).ToList();

			OldCollection.RemoveRange(RemoveItems);

			// Then recursivelly update every item.
			// An old item might move or a new item might be inserted in the middle or appended to the end.
			for (int i = 0; i < NewCollection.Count; i++)
			{
				IUniqueItem NewItem = NewCollection[i];

				if (i >= OldCollection.Count)
				{
					// appended to the end
					OldCollection.Add(NewItem);
				}
				else
				{
					// We removed the missing items, so this item is moved or has to be inserted
					if (!OldCollection[i].UniqueName.Equals(NewItem.UniqueName))
					{
						int OldIndex = -1;

						for (int j = i + 1; j < OldCollection.Count; j++)
						{
							if (OldCollection[j].UniqueName.Equals(NewItem.UniqueName))
							{
								OldIndex = j;
								break;
							}
						}

						if (OldIndex == -1)
						{
							// The item isn't found in the old collection
							OldCollection.Insert(i, NewItem);
						}
						else
						{
							// Move the item to it's new position
							OldCollection.Move(OldIndex, i);

							// If it's a collection, do the update recursivelly
							if (NewItem is ObservableItemGroup<IUniqueItem>)
							{
								UpdateGroupsItems(OldCollection[i] as ObservableItemGroup<IUniqueItem>, NewItem as ObservableItemGroup<IUniqueItem>);
							}
						}
					}
					else
					{
						// The item is in it's right place.
						// If it's a collection, do the update recursivelly
						if (NewItem is ObservableItemGroup<IUniqueItem>)
						{
							UpdateGroupsItems(OldCollection[i] as ObservableItemGroup<IUniqueItem>, NewItem as ObservableItemGroup<IUniqueItem>);
						}
					}
				}
			}
		}

	}
}
