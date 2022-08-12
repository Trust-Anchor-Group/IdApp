using IdApp.Helpers;
using IdApp.Services.Navigation;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contacts.Chat
{
	/// <summary>
	/// A page that displays a list of the current user's contacts.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	[QueryProperty(nameof(UniqueId), nameof(UniqueId))]
	public partial class ChatPage
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Views unique ID
		/// </summary>
		public string UniqueId
		{
			set
			{
				(this.ViewModel as ChatViewModel).UniqueId = value;
			}
		}
		/// <summary>
		/// Creates a new instance of the <see cref="ChatPage"/> class.
		/// </summary>
		public ChatPage()
		{
			this.navigationService = App.Instantiate<INavigationService>();
			this.ViewModel = new ChatViewModel();

			this.InitializeComponent();
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

			if (Device.RuntimePlatform == Device.Android)
			{
				App.Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
			}

			MessagingCenter.Subscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears, (sender, eargs) =>
			{
				if (this.ContainerView.TranslationY == 0)
				{
					double Bottom = 0;
					if (Device.RuntimePlatform == Device.iOS)
					{
						Thickness SafeInsets = this.On<iOS>().SafeAreaInsets();
						Bottom = SafeInsets.Bottom;
					}

					this.ContainerView.TranslationY -= eargs.KeyboardSize - Bottom;
				}
			});

			MessagingCenter.Subscribe<object>(this, Constants.MessagingCenter.KeyboardDisappears, (sender) =>
			{
				this.ContainerView.TranslationY = 0;
			});

			MessagingCenter.Subscribe<object>(this, Constants.MessagingCenter.ChatEditorFocus, (sender) =>
			{
				if (!this.EditorControl.IsFocused)
				{
					this.EditorControl.Focus();
				}
			});

			this.ContainerView.ResolveLayoutChanges();  // Strange Xamarin issue: https://github.com/xamarin/Xamarin.Forms/issues/15066
		}

		/// <inheritdoc/>
		protected override async Task OnDisappearingAsync()
		{
			MessagingCenter.Unsubscribe<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.KeyboardDisappears);
			MessagingCenter.Unsubscribe<object>(this, Constants.MessagingCenter.ChatEditorFocus);

			if (Device.RuntimePlatform == Device.Android)
			{
				App.Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Pan);
			}
			await base.OnDisappearingAsync();
			
		}

		private void OnEditorControlUnfocused(object Sender, FocusEventArgs e)
		{
			this.CollectionView.SelectedItem = null;
		}
	}
}
