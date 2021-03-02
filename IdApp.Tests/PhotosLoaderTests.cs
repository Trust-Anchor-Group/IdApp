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
            this.photos = new ObservableCollection<ImageSource>();
            this.sut = new PhotosLoader(
                this.logService.Object,
                this.networkService.Object,
                this.neuronService.Object,
                this.uiDispatcher.Object,
                this.imageCacheService.Object,
                this.photos);
            // Short circuit the begin invoke calls, so they're executed synchronously.
            this.uiDispatcher.Setup(x => x.BeginInvokeOnMainThread(It.IsAny<Action>())).Callback<Action>(x => x());
        }

        [TearDown]
        public void Teardown()
        {
            this.neuronContracts.Reset();
            this.networkService.Reset();
            this.imageCacheService.Reset();
            this.sut.CancelLoadPhotos();
        }

        [Test]
        public async Task LoadOnePhoto_ReturnsNull_WhenAttachmentIsNull()
        {
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream stream = await sut.LoadOnePhoto(null, SignWith.LatestApprovedIdOrCurrentKeys);
            Assert.IsNull(stream);
            MemoryStream cachedStream;
            this.imageCacheService.Verify(x => x.TryGet(It.IsAny<string>(), out cachedStream), Times.Never);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
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
            // Given
            const string url = "https://www.trustanchorgroup.com/photo.jpg";
            Attachment att = new Attachment { ContentType = "image/jpg", Url = url };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url, out cachedStream)).Returns(true);
            // When
            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreSame(cachedStream, stream);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task LoadOnePhoto_GetsPhotoFromServer_IfMissingFromCache()
        {
            // Given
            const string url = "https://www.trustanchorgroup.com/photo.jpg";
            Attachment att = new Attachment { ContentType = "image/jpg", Url = url };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url, out cachedStream)).Returns(false);
            MemoryStream serverStream = new MemoryStream();
            KeyValuePair<string, TemporaryFile> file = new KeyValuePair<string, TemporaryFile>(url, new TemporaryFile());
            this.neuronContracts.Setup(x => x.GetAttachment(url, It.IsAny<SignWith>(), It.IsAny<TimeSpan>())).ReturnsAsync(file);
            // When
            MemoryStream stream = await sut.LoadOnePhoto(att, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.NotNull(stream);
            // Verify cache miss
            this.imageCacheService.Verify(x => x.TryGet(url, out cachedStream), Times.Once);
            // Verify request
            this.neuronContracts.Verify(x => x.GetAttachment(url, It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Once);
            // Verify added to cache
            this.imageCacheService.Verify(x => x.Add(url, It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task LoadPhotos_DoesntLoad_WhenAttachmentsAreNull()
        {
            // Given
            this.networkService.SetupGet(x => x.IsOnline).Returns(false);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(null, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(0, this.photos.Count);
            MemoryStream cachedStream;
            this.imageCacheService.Verify(x => x.TryGet(It.IsAny<string>(), out cachedStream), Times.Never);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task LoadPhotos_DoesntLoad_WhenAttachmentsAreEmpty()
        {
            // Given
            this.networkService.SetupGet(x => x.IsOnline).Returns(false);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(new Attachment[0], SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(0, this.photos.Count);
            MemoryStream cachedStream;
            this.imageCacheService.Verify(x => x.TryGet(It.IsAny<string>(), out cachedStream), Times.Never);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task LoadPhotos_DoesntLoad_WhenNetworkIsOffline()
        {
            // Given
            const string url1 = "https://www.trustanchorgroup.com/photo1.jpg";
            const string url2 = "https://www.trustanchorgroup.com/photo2.jpg";
            Attachment[] attachments = new[]
            {
                new Attachment {ContentType = "image/jpg", Url = url1 },
                new Attachment {ContentType = "image/jpg", Url = url2 },
            };
            this.networkService.SetupGet(x => x.IsOnline).Returns(false);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(0, this.photos.Count);
        }

        [Test]
        public async Task LoadPhotos_DoesntLoad_WhenContractsIsOffline()
        {
            // Given
            const string url1 = "https://www.trustanchorgroup.com/photo1.jpg";
            const string url2 = "https://www.trustanchorgroup.com/photo2.jpg";
            Attachment[] attachments = new[]
            {
                new Attachment {ContentType = "image/jpg", Url = url1 },
                new Attachment {ContentType = "image/jpg", Url = url2 },
            };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(false);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(0, this.photos.Count);
        }

        [Test]
        public async Task LoadPhotos_GetsPhotoFromCache_IfItExists()
        {
            // Given
            const string url1 = "https://www.trustanchorgroup.com/photo1.jpg";
            const string url2 = "https://www.trustanchorgroup.com/photo2.jpg";
            Attachment[] attachments = new[]
            {
                new Attachment {ContentType = "image/jpg", Url = url1 },
                new Attachment {ContentType = "image/jpg", Url = url2 },
            };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream1 = new MemoryStream();
            MemoryStream cachedStream2 = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url1, out cachedStream1)).Returns(true);
            this.imageCacheService.Setup(x => x.TryGet(url2, out cachedStream2)).Returns(true);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(2, this.photos.Count);
            this.neuronContracts.Verify(x => x.GetAttachment(It.IsAny<string>(), It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Test]
        public async Task LoadPhotos_GetsPhotoFromServer_IfMissingFromCache()
        {
            // Given
            const string url1 = "https://www.trustanchorgroup.com/photo1.jpg";
            const string url2 = "https://www.trustanchorgroup.com/photo2.jpg";
            Attachment[] attachments = new[]
            {
                new Attachment {ContentType = "image/jpg", Url = url1 },
                new Attachment {ContentType = "image/jpg", Url = url2 },
            };
            this.networkService.SetupGet(x => x.IsOnline).Returns(true);
            this.neuronContracts.SetupGet(x => x.IsOnline).Returns(true);
            MemoryStream cachedStream1 = new MemoryStream();
            MemoryStream cachedStream2 = new MemoryStream();
            this.imageCacheService.Setup(x => x.TryGet(url1, out cachedStream1)).Returns(false);
            this.imageCacheService.Setup(x => x.TryGet(url2, out cachedStream2)).Returns(false);
            KeyValuePair<string, TemporaryFile> file1 = new KeyValuePair<string, TemporaryFile>(url1, new TemporaryFile());
            this.neuronContracts.Setup(x => x.GetAttachment(url1, It.IsAny<SignWith>(), It.IsAny<TimeSpan>())).ReturnsAsync(file1);
            KeyValuePair<string, TemporaryFile> file2 = new KeyValuePair<string, TemporaryFile>(url1, new TemporaryFile());
            this.neuronContracts.Setup(x => x.GetAttachment(url2, It.IsAny<SignWith>(), It.IsAny<TimeSpan>())).ReturnsAsync(file2);
            Assert.AreEqual(0, this.photos.Count);
            // When
            await sut.LoadPhotos(attachments, SignWith.LatestApprovedIdOrCurrentKeys);
            // Then
            Assert.AreEqual(2, this.photos.Count);
            // Verify cache miss
            this.imageCacheService.Verify(x => x.TryGet(url1, out cachedStream1), Times.Once);
            this.imageCacheService.Verify(x => x.TryGet(url2, out cachedStream2), Times.Once);
            // Verify request
            this.neuronContracts.Verify(x => x.GetAttachment(url1, It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Once);
            this.neuronContracts.Verify(x => x.GetAttachment(url2, It.IsAny<SignWith>(), It.IsAny<TimeSpan>()), Times.Once);
            // Verify added to cache
            this.imageCacheService.Verify(x => x.Add(url1, It.IsAny<Stream>()), Times.Once);
            this.imageCacheService.Verify(x => x.Add(url2, It.IsAny<Stream>()), Times.Once);
        }
    }
}