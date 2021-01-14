using NUnit.Framework;
using Tag.Sdk.Core.Models;
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

        private sealed class TestArg
        {
        }

        [Test]
        public void PushArgs1()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs(ta1);

            TestArg ta2;
            sut.PopArgs(out ta2);

            Assert.AreSame(ta1, ta2);
        }

        [Test]
        public void PushArgs1_Twice_ReturnsNull()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs(ta1);

            TestArg ta2;
            sut.PopArgs(out ta2);

            Assert.AreSame(ta1, ta2);

            sut.PopArgs(out ta2);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs1_InvalidCast()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs(ta1);

            TagProfile ta2;
            sut.PopArgs(out ta2);

            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs2()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs("Test", ta1);

            string s;
            TestArg ta2;
            sut.PopArgs(out s, out ta2);

            Assert.AreEqual("Test", s);
            Assert.AreSame(ta1, ta2);
        }

        [Test]
        public void PushArgs2_Twice_ReturnsNull()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs("Test", ta1);

            string s;
            TestArg ta2;
            sut.PopArgs(out s, out ta2);

            Assert.AreEqual("Test", s);
            Assert.AreSame(ta1, ta2);

            sut.PopArgs(out s, out ta2);
            Assert.IsNull(s);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs2_InvalidCast()
        {
            TestArg ta1 = new TestArg();
            sut.PushArgs("Test", ta1);

            string s;
            TagProfile ta2;
            sut.PopArgs(out s, out ta2);

            Assert.AreEqual("Test", s);
            Assert.IsNull(ta2);
        }

        [Test]
        public void PushArgs3()
        {
            TestArg ta1 = new TestArg();
            DomainModel m1 = new DomainModel("domain", "key", "secret");
            sut.PushArgs("Test", ta1, m1);

            string s;
            TestArg ta2;
            DomainModel m2;
            sut.PopArgs(out s, out ta2, out m2);

            Assert.AreEqual("Test", s);
            Assert.AreSame(ta1, ta2);
            Assert.AreSame(m1, m2);
        }

        [Test]
        public void PushArgs3_Twice_ReturnsNull()
        {
            TestArg ta1 = new TestArg();
            DomainModel m1 = new DomainModel("domain", "key", "secret");
            sut.PushArgs("Test", ta1, m1);

            string s;
            TestArg ta2;
            DomainModel m2;
            sut.PopArgs(out s, out ta2, out m2);

            Assert.AreEqual("Test", s);
            Assert.AreSame(ta1, ta2);
            Assert.AreSame(m1, m2);

            sut.PopArgs(out s, out ta2, out m2);
            Assert.IsNull(s);
            Assert.IsNull(ta2);
            Assert.IsNull(m2);
        }

        [Test]
        public void PushArgs3_InvalidCast()
        {
            TestArg ta1 = new TestArg();
            DomainModel m1 = new DomainModel("domain", "key", "secret");
            sut.PushArgs("Test", ta1, m1);

            string s;
            TestArg ta2;
            TagProfile m2;
            sut.PopArgs(out s, out ta2, out m2);

            Assert.AreEqual("Test", s);
            Assert.AreSame(ta1, ta2);
            Assert.IsNull(m2);
        }
    }
}