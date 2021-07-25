﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Waher.Content;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.ValidatePhoneNr
{
    /// <summary>
    /// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
    /// </summary>
    public class ValidatePhoneNrViewModel : RegistrationStepViewModel
	{
		private readonly INetworkService networkService;
		private string hostName;
		private string apiKey;
		private string apiSecret;
		private int portNumber;

		/// <summary>
		/// Creates a new instance of the <see cref="ValidatePhoneNrViewModel"/> class.
		/// </summary>
		/// <param name="tagProfile">The tag profile to work with.</param>
		/// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="logService">The log service.</param>
		public ValidatePhoneNrViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ISettingsService settingsService,
			INetworkService networkService,
			ILogService logService)
			: base(RegistrationStep.ValidatePhoneNr, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
		{
			this.networkService = networkService;
			this.ValidateCommand = new Command(async () => await this.Validate(), ValidateCanExecute);
			this.PhoneNumberCommand = new Command<string>(text => PhoneNumberTextEdited(text));
			this.Title = AppResources.EnterPhoneNumber;
		}

		/// <summary>
		/// Override this method to do view model specific setup when it's parent page/view appears on screen.
		/// </summary>
		protected override async Task DoBind()
		{
			await base.DoBind();

			try
			{
				(bool Success, object Result) = await this.networkService.TryRequest(async () => await InternetContent.PostAsync(
					new Uri("https://id.tagroot.io/ID/CountryCode.ws"), string.Empty,
					new KeyValuePair<string, string>("Accept", "application/json")));

				if (Success && Result is Dictionary<string, object> Response &&
					Response.TryGetValue("PhoneCode", out object Obj) && Obj is string PhoneCode)
				{
					this.PhoneNumber = PhoneCode;
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		#region Properties

		/// <summary>
		/// See <see cref="PhoneNumber"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberProperty =
			BindableProperty.Create("PhoneNumber", typeof(string), typeof(ValidatePhoneNrViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				ValidatePhoneNrViewModel viewModel = (ValidatePhoneNrViewModel)b;
				System.Diagnostics.Debug.WriteLine($"PhoneNumber changed from '{oldValue}' to '{newValue}'");
				viewModel.ValidateCommand.ChangeCanExecute();
			});

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNumber
		{
			get { return (string)GetValue(PhoneNumberProperty); }
			set { SetValue(PhoneNumberProperty, value); }
		}

		/// <summary>
		/// The command to bind to for validating the phone number.
		/// </summary>
		public ICommand ValidateCommand { get; }

		/// <summary>
		/// The command to bind to when editing the phone number text in the UI.
		/// </summary>
		public ICommand PhoneNumberCommand { get; }

		#endregion

		private async Task Validate()
		{
			if (!this.networkService.IsOnline)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			SetIsBusy(ValidateCommand);

			try
			{
				await InternetContent.PostAsync(new Uri("https://id.tagroot.io/ID/SendVerificationMessage.ws"), 
					new Dictionary<string, object>()
					{
						{ "Nr", this.PhoneNumber }
					});


				//string domainName = GetOperator();
				//(this.hostName, this.portNumber, this.isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(domainName);
				//
				//(bool succeeded, string errorMessage) = await this.NeuronService.TryConnect(domainName, this.isIpAddress, hostName, portNumber, Constants.LanguageCodes.Default, typeof(App).Assembly, null);
				//
				//UiDispatcher.BeginInvokeOnMainThread(async () =>
				//{
				//	this.SetIsDone(ValidateCommand);
				//
				//	if (succeeded)
				//	{
				//		bool DefaultConnectivity = hostName == domainName && portNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
				//
				//		this.TagProfile.SetDomain(domainName, DefaultConnectivity);
				//		this.OnStepCompleted(EventArgs.Empty);
				//	}
				//	else
				//	{
				//		this.LogService.LogException(new InvalidOperationException(), new KeyValuePair<string, string>("Connect", "Failed to connect"));
				//		await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
				//	}
				//});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(ValidateCommand);
			}
		}

		private bool ValidateCanExecute()
		{
			if (IsBusy) // is connecting
				return false;

			return !string.IsNullOrEmpty(this.PhoneNumber) && internationalPhoneNr.IsMatch(this.PhoneNumber);
		}

		private void PhoneNumberTextEdited(string text)
		{
			if (internationalPhoneNr.IsMatch(text))
				this.PhoneNumber = text;
			else
				this.PhoneNumber = string.Empty;
		}

		private static readonly Regex internationalPhoneNr = new Regex(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
	}
}