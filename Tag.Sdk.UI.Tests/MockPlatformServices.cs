using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Tag.Sdk.UI.Tests
{
    public class MockPlatformServices : IPlatformServices
    {
        private readonly Action<Action> invokeOnMainThread;
        private readonly Action<Uri> openUriAction;
        private readonly Func<Uri, CancellationToken, Task<Stream>> getStreamAsync;

        public MockPlatformServices(Action<Action> invokeOnMainThread = null, Action<Uri> openUriAction = null, Func<Uri, CancellationToken, Task<Stream>> getStreamAsync = null)
        {
            this.invokeOnMainThread = invokeOnMainThread;
            this.openUriAction = openUriAction;
            this.getStreamAsync = getStreamAsync;
        }

        public void BeginInvokeOnMainThread(Action action)
        {
            if (invokeOnMainThread == null)
                action();
            else
                invokeOnMainThread(action);
        }

        public Ticker CreateTicker()
        {
            return new MockTicker();
        }

        public Assembly[] GetAssemblies()
        {
            return new Assembly[0];
        }

        public string GetHash(string input)
        {
            throw new NotImplementedException();
        }

        public string GetMD5Hash(string input)
        {
            throw new NotImplementedException();
        }

        public double GetNamedSize(NamedSize size, Type targetElementType, bool useOldSizes)
        {
            switch (size)
            {
                case NamedSize.Default:
                    return 10;
                case NamedSize.Micro:
                    return 4;
                case NamedSize.Small:
                    return 8;
                case NamedSize.Medium:
                    return 12;
                case NamedSize.Large:
                    return 16;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size));
            }
        }

        public Color GetNamedColor(string name)
        {
            return Color.Black;
        }

        public Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (getStreamAsync == null)
                throw new NotImplementedException();
            return getStreamAsync(uri, cancellationToken);
        }

        public IIsolatedStorageFile GetUserStoreForApplication()
        {
            throw new NotImplementedException();
        }

        public void OpenUriAction(Uri uri)
        {
        }

        public void StartTimer(TimeSpan interval, Func<bool> callback)
        {
            Timer timer = null;

            void OnTimeout(object? o) =>
                BeginInvokeOnMainThread(() =>
                {
                    if (callback()) return;

                    timer.Dispose();
                });

            timer = new Timer(OnTimeout, null, interval, interval);
        }

        public void QuitApplication()
        {
        }

        public SizeRequest GetNativeSize(VisualElement view, double widthConstraint, double heightConstraint)
        {
            return new SizeRequest(new Size(1024, 768));
        }

        public bool IsInvokeRequired => false;
        public OSAppTheme RequestedTheme { get; }
        public string RuntimePlatform => "Android";
    }

    internal class MockTicker : Ticker
    {
        bool enabled;

        protected override void EnableTimer()
        {
            enabled = true;

            while (enabled)
            {
                SendSignals(16);
            }
        }

        protected override void DisableTimer()
        {
            enabled = false;
        }
    }
}