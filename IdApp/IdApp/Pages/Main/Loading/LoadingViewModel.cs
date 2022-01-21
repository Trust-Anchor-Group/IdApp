﻿using IdApp.Extensions;
using System.Threading.Tasks;
using IdApp.Services;
using Waher.Networking.XMPP;
using Xamarin.Forms;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;

namespace IdApp.Pages.Main.Loading
{
	/// <summary>
	/// The view model to bind to for a loading page, i.e. a page showing the loading/startup state of a Xamarin app.
	/// </summary>
	public class LoadingViewModel : NeuronViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="LoadingViewModel"/> class.
		/// </summary>
		protected internal LoadingViewModel()
			: base()
		{
		}

		/// <inheritdoc />
		protected override async Task DoBind()
		{
			await base.DoBind();
			IsBusy = true;
			this.DisplayConnectionText = this.TagProfile.Step > RegistrationStep.Account;
			this.NeuronService.Loaded += NeuronService_Loaded;
		}

		/// <inheritdoc />
		protected override async Task DoUnbind()
		{
			this.NeuronService.Loaded -= NeuronService_Loaded;
			await base.DoUnbind();
		}

		#region Properties

		/// <summary>
		/// See <see cref="DisplayConnectionText"/>
		/// </summary>
		public static readonly BindableProperty DisplayConnectionTextProperty =
			BindableProperty.Create("DisplayConnectionText", typeof(bool), typeof(LoadingViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user friendly connection text should be visible on screen or not.
		/// </summary>
		public bool DisplayConnectionText
		{
			get { return (bool)GetValue(DisplayConnectionTextProperty); }
			set { SetValue(DisplayConnectionTextProperty, value); }
		}

		#endregion

		/// <inheritdoc/>
		protected override void NeuronService_ConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() => SetConnectionStateAndText(e.State));
		}

		/// <inheritdoc/>
		protected override void SetConnectionStateAndText(XmppState state)
		{
			this.ConnectionStateText = state.ToDisplayText();
			this.ConnectionStateColor = new SolidColorBrush(state.ToColor());
			this.IsConnected = state == XmppState.Connected;
			this.StateSummaryText = (this.TagProfile.LegalIdentity?.State)?.ToString() + " - " + this.ConnectionStateText;
		}

		private void NeuronService_Loaded(object sender, LoadedEventArgs e)
		{
			if (e.IsLoaded)
			{
				this.IsBusy = false;

				this.UiSerializer.BeginInvokeOnMainThread(async () =>
				{
					if (this.TagProfile.IsComplete())
						await this.NavigationService.GoToAsync($"///{nameof(Main.MainPage)}");
					else
						await this.NavigationService.GoToAsync($"/{nameof(Registration.Registration.RegistrationPage)}");
				});
			}
		}
	}
}