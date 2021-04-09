using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.ViewModels;
using Tag.Neuron.Xamarin.UI.Views;

namespace Tag.Neuron.Xamarin.UI.Tests.Views
{
    public class ContentBasePageTests
    {
        private readonly Mock<ILogService> logService;
        private readonly Mock<ISettingsService> settingsService;
        private readonly Mock<IUiDispatcher> uiDispatcher;
        private readonly TestContentBasePage sut;
        private readonly TestContentBasePageViewModel viewModel;

        public ContentBasePageTests()
        {
            this.logService = new Mock<ILogService>();
            this.settingsService = new Mock<ISettingsService>();
            this.uiDispatcher = new Mock<IUiDispatcher>();
            MockForms.Init();
            this.viewModel = new TestContentBasePageViewModel();
            sut = new TestContentBasePage(this.logService.Object, this.settingsService.Object, this.uiDispatcher.Object)
            {
                BindingContext = this.viewModel
            };
        }

        [Test]
        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public async Task Appear_OnlyCallsRestoreState_IfSettingsAreAvailable(bool settingsAreAvailable, int expectedRestoreStateCount)
        {
            await this.viewModel.Bind();
            this.viewModel.Reset();
            this.settingsService.Setup(x => x.WaitInitDone()).Returns(Task.FromResult(settingsAreAvailable));
            this.sut.Appear();
            Assert.AreEqual(expectedRestoreStateCount, this.viewModel.RestoreStateCounter);
            await this.viewModel.Unbind();
        }

        [Test]
        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public async Task Disappear_OnlyCallsSaveState_IfSettingsAreAvailable(bool settingsAreAvailable, int expectedSaveStateCount)
        {
            await this.viewModel.Bind();
            this.viewModel.Reset();
            this.settingsService.Setup(x => x.WaitInitDone()).Returns(Task.FromResult(settingsAreAvailable));
            this.sut.Disappear();
            Assert.AreEqual(expectedSaveStateCount, this.viewModel.SaveStateCounter);
            await this.viewModel.Unbind();
        }

        #region Test Classes

        private sealed class TestContentBasePage : ContentBasePage
        {
            public TestContentBasePage(ILogService logService, ISettingsService settingsService, IUiDispatcher uiDispatcher)
                : base(logService, settingsService, uiDispatcher)
            {
            }

            public void Appear()
            {
                this.OnAppearing();
            }

            public void Disappear()
            {
                this.OnDisappearing();
            }
        }

        private sealed class TestContentBasePageViewModel : BaseViewModel
        {
            public int SaveStateCounter { get; private set; }
            public int RestoreStateCounter { get; private set; }

            public void Reset()
            {
                this.SaveStateCounter = 0;
                this.RestoreStateCounter = 0;
            }

            protected override Task DoSaveState()
            {
                this.SaveStateCounter++;
                return base.DoSaveState();
            }

            protected override Task DoRestoreState()
            {
                this.RestoreStateCounter++;
                return base.DoRestoreState();
            }
        }

        #endregion
    }
}