using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task SaveAndRestore_ReturnsCorrectValue()
        {
            string expected = "TAG";
            await sut.SaveState("key", expected);
            string actual = await sut.RestoreState<string>("key");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task Restore_ReturnsDefaultValue()
        {
            string val = "TAG";
            await sut.SaveState("key", val);
            string actual = await sut.RestoreState<string>("key2");
            Assert.IsNull(actual);
        }

        [Test]
        public async Task Restore_ReturnsSpecifiedDefaultValue()
        {
            const string customDefaultValue = "customDefaultValue";
            string val = "TAG";
            await sut.SaveState("key", val);
            string actual = await sut.RestoreState<string>("key2", customDefaultValue);
            Assert.AreEqual(customDefaultValue, actual);
        }

        [Test]
        public async Task RestoreStateWhere_ReturnsMatches()
        {
            const int nSettings = 3;
            for (int i = 0; i < nSettings; i++)
            {
                await sut.SaveState($"Key{i}", $"Val{i}");
            }
            await sut.SaveState("Foo", "Bar");

            List<(string Key, string Value)> actual = (await sut.RestoreStateWhere<string>(x => x.StartsWith("Key"))).ToList();
            Assert.AreEqual(nSettings, actual.Count);
            for (int i = 0; i < nSettings; i++)
            {
                Assert.AreEqual($"Key{i}", actual[i].Key);
            }
        }
    }
}