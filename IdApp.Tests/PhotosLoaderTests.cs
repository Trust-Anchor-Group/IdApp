using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using IdApp.Services;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
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

        [Test]
        public async Task LoadOnePhoto_GetsPhotoFromCache_IfItExists()
        {
            const string url = "https://www.trustanchorgroup.com/photo.jpg";
            Attachment att = new Attachment { ContentType = "image/jpg", Url = url };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url, out cachedStream)).Returns(true);
            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            Assert.AreSame(cachedStream, stream);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task LoadOnePhoto_GetsPhotoFromServer_IfMissingFromCache()
        {
            this.neuronContracts.Reset();
            this.networkService.Reset();
            this.imageCacheService.Reset();
            const string url = "https://www.trustanchorgroup.com/photo.jpg";
            Attachment att = new Attachment { ContentType = "image/jpg", Url = url };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url, out cachedStream)).Returns(false);
            MemoryStream serverStream = new MemoryStream();
            KeyValuePair<string, TemporaryFile> file = new KeyValuePair<string, TemporaryFile>(url, new TemporaryFile());
            this.neuronContracts.Setup(x => x.GetAttachment(url, It.IsAny<SignWith>(), It.IsAny<TimeSpan>())).ReturnsAsync(file);

            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            Assert.NotNull(stream);

            // Verify cache miss
            this.imageCacheService.Verify(x => x.TryGet(url, out cachedStream), Times.Once);
            // Verify request
            this.neuronContracts.Verify(x => x.GetAttachment(url, It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Once);
            // Verify added to cache
            this.imageCacheService.Verify(x => x.Add(url, It.IsAny<Stream>()), Times.Once);

        }
    }
}