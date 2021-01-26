namespace Tag.Neuron.Xamarin.Services
{
    public sealed class LegalIdentityAttachment
    {
        public LegalIdentityAttachment(string fileName, string contentType, byte[] data)
        {
            Filename = fileName;
            ContentType = contentType;
            Data = data;
            ContentLength = data?.Length ?? 0;
        }

        public string Filename { get; }
        public string ContentType { get; }
        public byte[] Data { get; }
        public long ContentLength { get; }
    }
}