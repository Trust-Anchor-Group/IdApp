namespace IdApp.AR
{
	interface IAudioItem
	{
		event EventHandler? MetadataRetrieved;

		string? FilePath { get; }
		TimeSpan? Duration { get; }
		TimeSpan? Position { get; }
	}
}
