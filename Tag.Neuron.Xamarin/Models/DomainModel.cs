namespace Tag.Neuron.Xamarin.Models
{
    public sealed class DomainModel
    {
        public DomainModel(string domainName, string key, string secret)
        {
            this.Name = domainName;
            this.Key = key;
            this.Secret = secret;
        }

        public string Name { get; }
        public string Key { get; }
        public string Secret { get; }
    }
}