using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;

namespace IdApp.Tests
{
    public class PhotosLoaderTests
    {
        private readonly Mock<ILogService> logService;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INeuronContracts> neuronContracts;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly Mock<IImageCacheService> imageCacheService;
        private readonly ObservableCollection<ImageSource> photos;
        private readonly PhotosLoader sut;

        public PhotosLoaderTests()
        {
            this.logService = new Mock<ILogService>();
            this.networkService = new Mock<INetworkService>();
            this.neuronService = new Mock<INeuronService>();
            this.neuronContracts = new Mock<INeuronContracts>();
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronService.SetupGet(x => x.Contracts).Returns(this.neuronContracts.Object);
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.uiDispatcher = new Mock<IUiDispatcher>();
            this.imageCacheService = new Mock<IImageCacheService>();
            this.sut = new PhotosLoader(
                this.logService.Object,
                this.networkService.Object,
                this.neuronService.Object,
                this.uiDispatcher.Object,
                this.imageCacheService.Object,
                this.photos);
        }

        [Test]
        public async Task LoadOnePhoto_ReturnsNull_WhenNetworkIsOffline()
        {
            Attachment att = new Attachment { ContentType = "image/jpg", Url = "https://www.trustanchorgroup.com/photo.jpg" };
            this.networkService.SetupGet(x => x.IsOnline).Returns(false);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            Assert.IsNull(stream);
        }

        [Test]
        public async Task LoadOnePhoto_ReturnsNull_WhenContractsIsOffline()
        {
            Attachment att = new Attachment { ContentType = "image/jpg", Url = "https://www.trustanchorgroup.com/photo.jpg" };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(false);
            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            Assert.IsNull(stream);
        }
    }
}