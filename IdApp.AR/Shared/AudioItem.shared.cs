using Xamarin.CommunityToolkit.ObjectModel;

namespace IdApp.AR
{
	public partial class AudioItem : ObservableObject, IAudioItem
	{
		public event EventHandler? ChangeUpdate;

		public bool IsPlaying { get; private set; }

		public string FilePath { get; private set; }

		public double Duration { get; private set; }

		public double Position { get; private set; }

		public AudioItem()
		{
			this.FilePath = string.Empty;
			this.Init(string.Empty);
		}

		public void SetIsPlaying(bool IsPlaying)
		{
			this.IsPlaying = IsPlaying;
			ChangeUpdate?.Invoke(this, EventArgs.Empty);
		}

		public void SetPosition(double Position)
		{
			this.Position = Position;
			ChangeUpdate?.Invoke(this, EventArgs.Empty);
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
			this.Duration = 0.5;
			this.Position = 0;
			this.FilePath = FilePath;
		}
	}
}
