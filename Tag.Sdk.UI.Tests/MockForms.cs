using Xamarin.Forms;

namespace Tag.Sdk.UI.Tests
{
    public static class MockForms
    {
        public static void Init()
        {
            Device.Info = new MockDeviceInfo();
            Device.PlatformServices = new MockPlatformServices();
        }
    }
}