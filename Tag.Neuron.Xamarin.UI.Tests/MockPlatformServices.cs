using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Tag.Neuron.Xamarin.UI.Tests
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
			if (invokeOnMainThread is null)
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
			return size switch
			{
				NamedSize.Default => 10,
				NamedSize.Micro => 4,
				NamedSize.Small => 8,
				NamedSize.Medium => 12,
				NamedSize.Large => 16,
				_ => throw new ArgumentOutOfRangeException(nameof(size)),
			};
		}

		public Color GetNamedColor(string name)
		{
			return Color.Black;
		}

		public Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
		{
			if (getStreamAsync is null)
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
			Task.Delay(interval).ContinueWith(_ => callback());
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