namespace IdApp.AR
{
	interface IAudioItem
	{
		void Initialise(string FilePath);
		void SetIsPlaying(bool IsPlaying);
		void SetPosition(double Position);

		event EventHandler? ChangeUpdate;

		bool IsPlaying { get; }
		string FilePath { get; }
		double Duration { get; }
		double Position { get; }
	}
}
