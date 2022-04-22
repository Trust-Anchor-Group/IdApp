using System.Collections.ObjectModel;

namespace IdApp.Pages.Wallet
{
    public interface IItemGroup
    {
    }

    public interface IItemGroupCollection
    {
        public string Name { get; set; }
    }

    public class ItemGroupCollection<T> : ObservableCollection<T>, IItemGroupCollection where T : IItemGroup
    {
        public string Name { get; set; }

        public ItemGroupCollection(string name, ObservableCollection<T> Items) : base(Items)
        {
            Name = name;
        }
    }
}
