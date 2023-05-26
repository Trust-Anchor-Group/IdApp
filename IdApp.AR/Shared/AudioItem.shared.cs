using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.AR
{
	public partial class AudioItem : ObservableObject, IAudioItem
	{
		public event EventHandler? ChangeUpdate;

		public bool IsPlaying { get; set; }

		public string FilePath { get; private set; }

		public TimeSpan? Duration { get; private set; }

		public int Position { get; set; }

		public AudioItem()
		{
			this.FilePath = string.Empty;
			this.Init(string.Empty);
		}

		public void Initialise(string FilePath)
		{
			this.Init(FilePath);

			ChangeUpdate?.Invoke(this, EventArgs.Empty);

			if (!string.IsNullOrEmpty(FilePath))
			{
				Task.Run(this.ExtractMetadata);
			}
		}

		private void Init(string FilePath)
		{
			this.IsPlaying = false;
			this.Duration = null;
			this.Position = 0;
			this.FilePath = FilePath;
		}
	}
}
