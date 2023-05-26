namespace IdApp.AR
{
	interface IAudioItem
	{
		event EventHandler? MetadataRetrieved;

		bool IsPlaying { get; }
		string FilePath { get; }
		TimeSpan? Duration { get; }
		int Position { get; }
	}
}
