namespace IdApp.AR
{
	interface IAudioItem
	{
		void Initialise(string FilePath);

		event EventHandler? ChangeUpdate;

		bool IsPlaying { get; }
		string FilePath { get; }
		TimeSpan? Duration { get; }
		int Position { get; }
	}
}
