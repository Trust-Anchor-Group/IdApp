﻿using IdApp.Helpers;
using IdApp.Services.Navigation;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ChatPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates a new instance of the <see cref="ChatPage"/> class.
		/// </summary>
		public ChatPage()
		{
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ChatViewModel();
			
			InitializeComponent();
		}

		/// <summary>
		/// Overrides the back button behavior to handle navigation internally instead.
		/// </summary>
		/// <returns>Whether or not the back navigation was handled</returns>
		protected override bool OnBackButtonPressed()
		{
			this.navigationService.GoBackAsync();
			return true;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearingAsync()
		{
			await base.OnAppearingAsync();

			MessagingCenter.Subscribe<object, KeyboardAppearEventArgs>(this, Constants.iOSKeyboardAppears, (sender, eargs) =>
			{
				if (ContainerView.TranslationY == 0)
				{
					double Bottom = 0;
					if (Device.RuntimePlatform == Device.iOS)
					{
						Thickness SafeInsets = On<iOS>().SafeAreaInsets();
						Bottom = SafeInsets.Bottom;
					}

					ContainerView.TranslationY -= eargs.KeyboardSize - Bottom;
				}
			});
			MessagingCenter.Subscribe<object, string>(this, Constants.iOSKeyboardDisappears, (sender, eargs) =>
			{
				ContainerView.TranslationY = 0;
			});

			this.ContainerView.ResolveLayoutChanges();  // Strange Xamarin issue: https://github.com/xamarin/Xamarin.Forms/issues/15066
		}

		protected override async Task OnDisappearingAsync()
		{
			MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.iOSKeyboardAppears);
			MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.iOSKeyboardDisappears);

			await base.OnDisappearingAsync();
		}
	}
}