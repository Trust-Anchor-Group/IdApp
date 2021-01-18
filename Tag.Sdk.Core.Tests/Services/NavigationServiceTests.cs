using NUnit.Framework;
using Tag.Sdk.Core.Services;

namespace Tag.Sdk.Core.Tests.Services
{
    public class NavigationServiceTests
    {
        private readonly INavigationService sut;

        public NavigationServiceTests()
        {
            sut = new NavigationService();
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

            TestArgs1 ta2;
            bool succeeded = sut.TryPopArgs(out ta2);
            Assert.IsTrue(succeeded);
            Assert.AreSame(ta1, ta2);
        }

        [Test]
        public void PopArgs_Twice_ReturnsNullTheSecondTime()
        {
            TestArgs1 ta1 = new TestArgs1();
            sut.PushArgs(ta1);

            TestArgs1 ta2;
            bool succeeded = sut.TryPopArgs(out ta2);
            Assert.IsTrue(succeeded);
            Assert.AreSame(ta1, ta2);

            succeeded = sut.TryPopArgs(out ta2);
            Assert.IsFalse(succeeded);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs_ReturnsNull_IfPushedDiffersFromPopped()
        {
            TestArgs1 ta1 = new TestArgs1();
            sut.PushArgs(ta1);

            TestArgs2 ta2;
            bool succeeded = sut.TryPopArgs(out ta2);
            Assert.IsFalse(succeeded);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs_PushNull_ReturnsNull()
        {
            sut.PushArgs((NavigationArgs)null);
            TestArgs1 ta1;
            bool succeeded = sut.TryPopArgs(out ta1);
            Assert.IsFalse(succeeded);
            Assert.IsNull(ta1);
        }
    }
}