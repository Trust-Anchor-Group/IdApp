using System.Collections.Generic;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Extensions;

namespace Tag.Neuron.Xamarin.Tests.Extensions
{
    public class ListExtensionsTests
    {
        [Test]
        [TestCase(1, 1, true)]
        [TestCase(1, 2, false)]
        [TestCase(2, 1, false)]
        public void HasSameContentAs(int l1Content, int l2Content, bool expected)
        {
            List<int> l1 = new List<int> { l1Content };
            List<int> l2 = new List<int> { l2Content };
            Assert.AreEqual(expected, l1.HasSameContentAs(l2));
        }

        [Test]
        [TestCase(1, 1, 1, false)]
        [TestCase(1, 2, 1, false)]
        [TestCase(2, 1, 1, false)]
        [TestCase(2, 1, 2, false)]
        [TestCase(1, 2, 2, false)]
        public void HasSameContentAs_DifferentLength(int l1Content1, int l1Content2, int l2Content1, bool expected)
        {
            List<int> l1 = new List<int> { l1Content1, l1Content2 };
            List<int> l2 = new List<int> { l2Content1 };
            Assert.AreEqual(expected, l1.HasSameContentAs(l2));
        }

        [Test]
        [TestCase(1, 1, 1, 1, true)]
        [TestCase(1, 2, 1, 2, true)]
        [TestCase(2, 1, 1, 2, true)]
        [TestCase(2, 1, 2, 1, true)]
        [TestCase(1, 2, 2, 1, true)]
        public void HasSameContentAs_SameLength(int l1Content1, int l1Content2, int l2Content1, int l2Content2, bool expected)
        {
            List<int> l1 = new List<int> { l1Content1, l1Content2 };
            List<int> l2 = new List<int> { l2Content1, l2Content2 };
            Assert.AreEqual(expected, l1.HasSameContentAs(l2));
        }
    }
}