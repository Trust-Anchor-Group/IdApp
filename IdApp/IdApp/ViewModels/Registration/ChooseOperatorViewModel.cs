using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Tag.Neuron.Xamarin.UI.Extensions;
using Xamarin.Forms;

namespace IdApp.ViewModels.Registration
{
    /// <summary>
    /// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
    /// </summary>
    public class ChooseOperatorViewModel : RegistrationStepViewModel
	{
		private readonly INetworkService networkService;
		private string hostName;
		private int portNumber;
		private bool isIpAddress;

		/// <summary>
		/// Creates a new instance of the <see cref="ChooseOperatorViewModel"/> class.
		/// </summary>
		/// <param name="tagProfile">The tag profile to work with.</param>
		/// <param name="uiDispatcher">The UI dispatcher for alerts.</param>
		/// <param name="neuronService">The Neuron service for XMPP communication.</param>
		/// <param name="navigationService">The navigation service to use for app navigation</param>
		/// <param name="settingsService">The settings service for persisting UI state.</param>
		/// <param name="networkService">The network service for network access.</param>
		/// <param name="logService">The log service.</param>
		public ChooseOperatorViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService,
			ISettingsService settingsService,
			INetworkService networkService,
			ILogService logService)
			: base(RegistrationStep.Operator, tagProfile, uiDispatcher, neuronService, navigationService, settingsService, logService)
		{
			this.networkService = networkService;
			this.Operators = new ObservableCollection<string>();
			this.ConnectCommand = new Command(async () => await Connect(), ConnectCanExecute);
			this.ManualOperatorCommand = new Command<string>(async text => await ManualOperatorTextEdited(text));
			this.PopulateOperators();
			this.Title = AppResources.SelectOperator;
		}

		#region Properties

		/// <summary>
		/// The list of operators a user can choose from.
		/// </summary>
		public ObservableCollection<string> Operators { get; }

		/// <summary>
		/// See <see cref="SelectedOperator"/>
		/// </summary>
		public static readonly BindableProperty SelectedOperatorProperty =
			BindableProperty.Create("SelectedOperator", typeof(string), typeof(ChooseOperatorViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				ChooseOperatorViewModel viewModel = (ChooseOperatorViewModel)b;
				string selectedOperator = (string)newValue;
				viewModel.ChooseOperatorFromList = (viewModel.Operators.Count > 0) && (selectedOperator != AppResources.OperatorDomainOther);
				viewModel.ConnectCommand.ChangeCanExecute();
			});

		/// <summary>
		/// The selected operator from the list of <see cref="Operators"/>, if any.
		/// </summary>
		public string SelectedOperator
		{
			get { return (string)GetValue(SelectedOperatorProperty); }
			set { SetValue(SelectedOperatorProperty, value); }
		}

		/// <summary>
		/// See <see cref="ManualOperator"/>
		/// </summary>
		public static readonly BindableProperty ManualOperatorProperty =
			BindableProperty.Create("ManualOperator", typeof(string), typeof(ChooseOperatorViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				ChooseOperatorViewModel viewModel = (ChooseOperatorViewModel)b;
				System.Diagnostics.Debug.WriteLine($"ManualOperator changed from '{oldValue}' to '{newValue}'");
				viewModel.ConnectCommand.ChangeCanExecute();
			});

		/// <summary>
		/// The manually entered operator, if one isn't chosen from the list of <see cref="Operators"/>.
		/// </summary>
		public string ManualOperator
		{
			get { return (string)GetValue(ManualOperatorProperty); }
			set { SetValue(ManualOperatorProperty, value); }
		}

		/// <summary>
		/// <see cref="ChooseOperatorFromList"/>
		/// </summary>
		public static readonly BindableProperty ChooseOperatorFromListProperty =
			BindableProperty.Create("ChooseOperatorFromList", typeof(bool), typeof(ChooseOperatorViewModel), default(bool));

		/// <summary>
		/// <see cref="ChooseOperatorFromListProperty"/>
		/// </summary>
		public bool ChooseOperatorFromList
		{
			get { return (bool)GetValue(ChooseOperatorFromListProperty); }
			set { SetValue(ChooseOperatorFromListProperty, value); }
		}

		private string GetOperator()
		{
			return !string.IsNullOrWhiteSpace(SelectedOperator) && SelectedOperator != AppResources.OperatorDomainOther ? SelectedOperator : ManualOperator;
		}

		/// <summary>
		/// The command to bind to for connecting to the <see cref="SelectedOperator"/>.
		/// </summary>
		public ICommand ConnectCommand { get; }

		/// <summary>
		/// The command to bind to when editing the manual operator text in the UI.
		/// </summary>
		public ICommand ManualOperatorCommand { get; }

		#endregion

		private async Task Connect()
		{
			if (!this.networkService.IsOnline)
			{
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, AppResources.NetworkSeemsToBeMissing);
				return;
			}

			SetIsBusy(ConnectCommand);

			try
			{
				string domainName = GetOperator();
				(this.hostName, this.portNumber, this.isIpAddress) = await this.networkService.LookupXmppHostnameAndPort(domainName);

				(bool succeeded, string errorMessage) = await this.NeuronService.TryConnect(domainName, this.isIpAddress, hostName, portNumber, Constants.LanguageCodes.Default, typeof(App).Assembly, null);

				UiDispatcher.BeginInvokeOnMainThread(async () =>
				{
					this.SetIsDone(ConnectCommand);

					if (succeeded)
					{
						this.TagProfile.SetDomain(this.GetOperator());
						this.OnStepCompleted(EventArgs.Empty);
					}
					else
					{
						this.LogService.LogException(new InvalidOperationException(), new KeyValuePair<string, string>("Connect", "Failed to connect"));
						await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, errorMessage, AppResources.Ok);
					}
				});
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiDispatcher.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
			}
			finally
			{
				this.BeginInvokeSetIsDone(ConnectCommand);
			}
		}

		private bool ConnectCanExecute()
		{
			if (IsBusy) // is connecting
			{
				return false;
			}

			if (!string.IsNullOrWhiteSpace(this.SelectedOperator) && this.Operators.Contains(this.SelectedOperator) && this.SelectedOperator != AppResources.OperatorDomainOther)
			{
				return true;
			}

			if (!string.IsNullOrWhiteSpace(ManualOperator))
			{
				return true;
			}

			return false;
		}

		private async Task ManualOperatorTextEdited(string text)
		{
			if (await IsValidDomainName(text))
			{
				ManualOperator = text;
			}
			else
			{
				ManualOperator = string.Empty;
			}
		}

		private async Task<bool> IsValidDomainName(string name)
		{
			if (!IPAddress.TryParse(name, out IPAddress _))
			{
				string[] parts = name.Split('.');
				if (parts.Length <= 1)
					return false;

				foreach (string part in parts)
				{
					if (string.IsNullOrWhiteSpace(part))
						return false;

					foreach (char ch in part)
					{
						if (!char.IsLetter(ch) && !char.IsDigit(ch) && ch != '-')
							return false;
					}
				}
			}

			(string host, int port, bool isIp) = await this.networkService.LookupXmppHostnameAndPort(name);

			if (string.IsNullOrWhiteSpace(host))
				return false;

			this.hostName = host;
			this.portNumber = port;
			this.isIpAddress = isIp;

			return true;
		}

		private void PopulateOperators()
		{
			int selectedIndex = -1;
			int i = 0;
			foreach (string domain in this.TagProfile.Domains)
			{
				this.Operators.Add(domain);

				if (domain == this.TagProfile.Domain)
				{
					selectedIndex = i;
				}
				i++;
			}
			this.Operators.Add(AppResources.OperatorDomainOther);

			if (selectedIndex >= 0)
			{
				this.SelectedOperator = this.Operators[selectedIndex];
			}

			if (!string.IsNullOrWhiteSpace(this.TagProfile.Domain))
			{
				if (selectedIndex >= 0)
				{
					this.SelectedOperator = this.Operators[selectedIndex];
				}
				else
				{
					this.ManualOperator = this.TagProfile.Domain;
				}
			}
			else
			{
				SelectedOperator = string.Empty;
			}
		}
	}
}