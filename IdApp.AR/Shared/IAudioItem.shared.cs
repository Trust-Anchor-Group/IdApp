namespace IdApp.AR
{
	interface IAudioItem
	{
		void Initialise(string FilePath);

		event EventHandler? ChangeUpdate;

		bool IsPlaying { get; }
		string FilePath { get; }
		double Duration { get; }
		double Position { get; }
	}
}
