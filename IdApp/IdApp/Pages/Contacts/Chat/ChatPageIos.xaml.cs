using IdApp.Helpers;
using IdApp.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
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
	[QueryProperty(nameof(UniqueId), nameof(UniqueId))]
	public partial class ChatPageIos
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
		/// Creates a new instance of the <see cref="ChatPageIos"/> class.
		/// </summary>
		public ChatPageIos()
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

			await base.OnDisappearingAsync();
		}

		private void OnEditorControlUnfocused(object sender, FocusEventArgs e)
		{
			this.CollectionView.SelectedItem = null;
		}

		private void ViewCell_Appearing(object Sender, EventArgs EventArgs)
		{
			ViewCell ViewCell = (ViewCell)Sender;
			ViewCell.Appearing -= this.ViewCell_Appearing;

			Image Image = ViewCell.View.Descendants().OfType<Image>().FirstOrDefault();
			if (Image != null)
			{
				ImageSizeChangedHandler SizeChangedHandler = new(new WeakReference<ViewCell>(ViewCell));
				Image.SizeChanged += SizeChangedHandler.HandleSizeChanged;
			}
		}

		private class ImageSizeChangedHandler
		{
			private readonly WeakReference<ViewCell> weakViewCell;

			public ImageSizeChangedHandler(WeakReference<ViewCell> WeakViewCell)
			{
				this.weakViewCell = WeakViewCell;
			}

			public void HandleSizeChanged(object Sender, EventArgs EventArgs)
			{
				if (this.weakViewCell.TryGetTarget(out ViewCell ViewCell))
				{
					ViewCell.ForceUpdateSize();
				}
			}
		}
	}
}
