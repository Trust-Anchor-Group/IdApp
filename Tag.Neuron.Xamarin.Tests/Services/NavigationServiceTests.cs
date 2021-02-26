using Moq;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Services;

namespace Tag.Neuron.Xamarin.Tests.Services
{
    public class NavigationServiceTests
    {
        private readonly NavigationService sut;

        public NavigationServiceTests()
        {
            sut = new NavigationService(new Mock<ILogService>().Object, new Mock<IUiDispatcher>().Object);
        }

        private sealed class TestArgs1 : NavigationArgs
        {
        }

        private sealed class TestArgs2 : NavigationArgs
        {
        }

        [Test]
        public void PushArgs_ReturnsSameAsPopped()
        {
            TestArgs1 ta1 = new TestArgs1();
            sut.PushArgs(ta1);

            bool succeeded = sut.TryPopArgs(out TestArgs1 ta2);
            Assert.IsTrue(succeeded);
            Assert.AreSame(ta1, ta2);
        }

        [Test]
        public void PopArgs_Twice_ReturnsSameArgsTheSecondTime()
        {
            TestArgs1 ta1 = new TestArgs1();
            sut.PushArgs(ta1);

            bool succeeded = sut.TryPopArgs(out TestArgs1 ta2);
            Assert.IsTrue(succeeded);
            Assert.AreSame(ta1, ta2);

            succeeded = sut.TryPopArgs(out ta2);
            Assert.IsTrue(succeeded);
            Assert.AreSame(ta1, ta2);
        }

        [Test]
        public void PushArgs_ReturnsNull_IfPushedDiffersFromPopped()
        {
            TestArgs1 ta1 = new TestArgs1();
            sut.PushArgs(ta1);

            bool succeeded = sut.TryPopArgs(out TestArgs2 ta2);
            Assert.IsFalse(succeeded);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs_PushNull_ReturnsNull()
        {
            sut.PushArgs((NavigationArgs)null);
            bool succeeded = sut.TryPopArgs(out TestArgs1 ta1);
            Assert.IsFalse(succeeded);
            Assert.IsNull(ta1);
        }
    }
}