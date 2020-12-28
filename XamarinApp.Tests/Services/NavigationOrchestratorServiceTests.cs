using Moq;
using NUnit.Framework;
using Tag.Sdk.Core.Services;
using XamarinApp.Services;

namespace XamarinApp.Tests.Services
{
    public class NavigationOrchestratorServiceTests
    {
        private readonly TagProfile tagProfile;
        private readonly Mock<INeuronService> neuronService;
        private readonly Mock<INetworkService> networkService;
        private readonly Mock<INavigationService> navigationService;
        private readonly NavigationOrchestratorService sut;

        public NavigationOrchestratorServiceTests()
        {
            this.tagProfile = new TagProfile();
            this.neuronService = new Mock<INeuronService>();
            this.networkService = new Mock<INetworkService>();
            this.navigationService = new Mock<INavigationService>();
            this.sut = new NavigationOrchestratorService(this.tagProfile, this.neuronService.Object, this.networkService.Object, this.navigationService.Object);
        }

        [SetUp]
        public void Setup()
        {
            this.sut.Load();
        }

        [TearDown]
        public void TearDown()
        {
            this.sut.Unload();
        }

        [Test]
        public void DownloadsLegalIdentity()
        {
            this.neuronService.Raise(x => x.Loaded += null, new LoadedEventArgs(true));
        }
    }
}