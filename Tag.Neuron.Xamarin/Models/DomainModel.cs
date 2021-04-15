namespace Tag.Neuron.Xamarin.Models
{
    /// <summary>
    /// The data model for Neuron domains.
    /// </summary>
    public sealed class DomainModel
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DomainModel"/> class.
        /// </summary>
        /// <param name="domainName">The domain name to use.</param>
        /// <param name="key">A key for the domain.</param>
        /// <param name="secret">The domain secret.</param>
        public DomainModel(string domainName, string key, string secret)
        {
            this.Name = domainName;
            this.Key = key;
            this.Secret = secret;
        }
        /// <summary>
        /// The domain name to use.
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// A key for the domain.
        /// </summary>
        public string Key { get; }
        
        /// <summary>
        /// The domain secret.
        /// </summary>
        public string Secret { get; }
    }
}