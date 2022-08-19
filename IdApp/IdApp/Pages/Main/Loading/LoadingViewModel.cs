using System;
using System.Threading.Tasks;
using IdApp.Extensions;
using IdApp.Services;
using IdApp.Services.Tag;
using IdApp.Services.Xmpp;
using Waher.Networking.XMPP;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Loading
{
	/// <summary>
	/// The view model to bind to for a loading page, i.e. a page showing the loading/startup state of a Xamarin app.
	/// </summary>
	public class LoadingViewModel : XmppViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="LoadingViewModel"/> class.
		/// </summary>
		protected internal LoadingViewModel()
			: base()
		{
		}

		/// <inheritdoc />
		protected override async Task OnAppearing()
		{
			await base.OnInitialize();

			this.IsBusy = true;
			this.DisplayConnectionText = this.TagProfile.Step > RegistrationStep.Account;
			this.XmppService.Loaded += this.XmppService_Loaded;
		}

		/// <inheritdoc />
		protected override async Task OnDisappearing()
		{
			this.XmppService.Loaded -= this.XmppService_Loaded;

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="DisplayConnectionText"/>
		/// </summary>
		public static readonly BindableProperty DisplayConnectionTextProperty =
			BindableProperty.Create(nameof(DisplayConnectionText), typeof(bool), typeof(LoadingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user friendly connection text should be visible on screen or not.
		/// </summary>
		public bool DisplayConnectionText
		{
			get => (bool)this.GetValue(DisplayConnectionTextProperty);
			set => this.SetValue(DisplayConnectionTextProperty, value);
		}

		#endregion

		/// <inheritdoc/>
		protected override void XmppService_ConnectionStateChanged(object Sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() => this.SetConnectionStateAndText(e.State));
		}

		/// <inheritdoc/>
		protected override void SetConnectionStateAndText(XmppState state)
		{
			this.ConnectionStateText = state.ToDisplayText();
			this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
			this.IsConnected = state == XmppState.Connected;
			this.StateSummaryText = (this.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
		}

		private void XmppService_Loaded(object Sender, LoadedEventArgs e)
		{
			try
			{
				if (e.IsLoaded)
				{
					this.IsBusy = false;

					// XmppService_Loaded method might be called from DoBind method, which in turn is called from OnAppearing method.
					// We cannot update the main page while some OnAppearing is still running (well, we can technically but there will be chaos).
					// Therefore, do not await this method and do not call it synchronously, even if we are already on the main thread.
					this.UiSerializer.BeginInvokeOnMainThread(async () =>
					{
						try
						{
							if (this.TagProfile.IsComplete())
								await App.Current.SetAppShellPageAsync();
							else
								await App.Current.SetRegistrationPageAsync();
						}
						catch (Exception Exception)
						{
							this.LogService?.LogException(Exception);
						}
					});
				}
			}
			catch (Exception Exception)
			{
				this.LogService?.LogException(Exception);
			}
		}
	}
}
