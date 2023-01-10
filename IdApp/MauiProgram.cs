using Microsoft.Extensions.Logging;

namespace IdApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder AppBuilder = MauiApp.CreateBuilder();

		AppBuilder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		AppBuilder.Logging.AddDebug();
#endif

		AppBuilder.Services.AddLocalization();

		return AppBuilder.Build();
	}
}
