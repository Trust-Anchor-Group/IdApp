using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Services;

namespace Tag.Neuron.Xamarin.Tests.Services
{
    public class SettingsServiceTests
    {
        private readonly SettingsService sut;

        public SettingsServiceTests()
        {
            sut = new SettingsService();
        }

        [Test]
        public void SaveAndRestore_ReturnsCorrectValue()
        {
            string expected = "TAG";
            sut.SaveState("key", expected);
            string actual = sut.RestoreState<string>("key");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Restore_ReturnsDefaultValue()
        {
            string val = "TAG";
            sut.SaveState("key", val);
            string actual = sut.RestoreState<string>("key2");
            Assert.IsNull(actual);
        }

        [Test]
        public void Restore_ReturnsSpecifiedDefaultValue()
        {
            const string customDefaultValue = "customDefaultValue";
            string val = "TAG";
            sut.SaveState("key", val);
            string actual = sut.RestoreState<string>("key2", customDefaultValue);
            Assert.AreEqual(customDefaultValue, actual);
        }

        [Test]
        public void RestoreStateWhere_ReturnsMatches()
        {
            const int nSettings = 3;
            for (int i = 0; i < nSettings; i++)
            {
                sut.SaveState($"Key{i}", $"Val{i}");
            }
            sut.SaveState("Foo", "Bar");

            List<(string Key, string Value)> actual = sut.RestoreStateWhere<string>(x => x.StartsWith("Key")).ToList();
            Assert.AreEqual(nSettings, actual.Count);
            for (int i = 0; i < nSettings; i++)
            {
                Assert.AreEqual($"Key{i}", actual[i].Key);
            }
        }
    }
}