using SQLite;

namespace Tag.Neuron.Xamarin.Services
{
    internal sealed class Setting
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}