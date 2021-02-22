using NUnit.Framework;

namespace Tag.Neuron.Xamarin.Tests
{
    public class ConstantsTests
    {
        [Test]
        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase("iotid", null)]
        [TestCase("iotid:", "iotid")]
        [TestCase("iotid:123", "iotid")]
        [TestCase("ioTID:123", "iotid")]
        [TestCase("iotsc", null)]
        [TestCase("iotsc:", "iotsc")]
        [TestCase("iotsc:123", "iotsc")]
        [TestCase("ioTSC:123", "iotsc")]
        public void IoTSchemes_GetScheme(string code, string expected)
        {
            string actual = Constants.UriSchemes.GetScheme(code);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase("iotid", null)]
        [TestCase("iotid:", "")]
        [TestCase("iotid:123", "123")]
        [TestCase("ioTID:123", "123")]
        [TestCase("iotsc", null)]
        [TestCase("iotsc:", "")]
        [TestCase("iotsc:123", "123")]
        [TestCase("ioTSC:123", "123")]
        public void IoTSchemes_GetCode(string code, string expected)
        {
            string actual = Constants.UriSchemes.GetCode(code);
            Assert.AreEqual(expected, actual);
        }
    }
}