using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.AR
{
	public class AudioItem : ObservableObject, IAudioItem
	{
		public AudioItem(string path)
		{
			throw new NotImplementedException();
		}

		public string FilePath => throw new NotImplementedException();

		public TimeSpan? Duration => throw new NotImplementedException();

		public TimeSpan Position => throw new NotImplementedException();

		public event EventHandler? MetadataRetrieved;
	}
}
