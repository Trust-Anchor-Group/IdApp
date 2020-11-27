using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage
	{
		private readonly Dictionary<string, Contract> contracts = new Dictionary<string, Contract>();
		private readonly TagProfile tagProfile;
		private readonly bool createdContracts;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;

		public MyContractsPage(bool createdContracts)
		{
			this.tagProfile = DependencyService.Resolve<TagProfile>();
			this.createdContracts = createdContracts;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
			InitializeComponent();

			this.LoadMyContracts();
		}

		private async void LoadMyContracts()
		{
			try
			{
				string[] contractIds;

				if (this.createdContracts)
					contractIds = await this.contractsService.GetCreatedContractsAsync();
				else
					contractIds = await this.contractsService.GetSignedContractsAsync();

				TapGestureRecognizer tapGestureRecognizer = new TapGestureRecognizer();
				tapGestureRecognizer.Tapped += async (sender, e) =>
				{
					if (sender is StackLayout layout && 
						!string.IsNullOrEmpty(layout.StyleId) &&
						this.contracts.TryGetValue(layout.StyleId, out Contract contract))
					{
						await this.navigationService.PushAsync(new ViewContractPage(contract, false));
					}
				};

				if (contractIds.Length == 0)
				{
					await Device.InvokeOnMainThreadAsync(() =>
					{
						StackLayout layout = this.AddCell();

						layout.Children.Add(new Label()
						{
							Text = "No contracts found."
						});
					});
				}
				else
				{
					foreach (string contractId in contractIds)
					{
						Contract contract = await this.contractsService.GetContractAsync(contractId);
						this.contracts[contractId] = contract;

						await Device.InvokeOnMainThreadAsync(() =>
						{
							StackLayout layout = this.AddCell();
							layout.StyleId = contractId;

							layout.Children.Add(new Label()
							{
								Text = contract.Created.ToString() + ":",
								LineBreakMode = LineBreakMode.NoWrap
							});

							layout.Children.Add(new Label()
							{
								Text = contract.ContractId + " (" + contract.ForMachinesNamespace + "#" + contract.ForMachinesLocalName + ")",
								TextType = TextType.Text,
								FontAttributes = FontAttributes.Bold,
								LineBreakMode = LineBreakMode.CharacterWrap
							});

							layout.GestureRecognizers.Add(tapGestureRecognizer);
						});
					}
				}
			}
			catch (Exception ex)
			{
				await Device.InvokeOnMainThreadAsync(() =>
				{
					StackLayout layout = this.AddCell();

					layout.Children.Add(new Label()
					{
						Text = ex.Message,
						TextColor = Color.Red,
						LineBreakMode = LineBreakMode.WordWrap
					});
				});
			}
		}

		private StackLayout AddCell()
		{
			StackLayout layout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal
			};

			ViewCell viewCell = new ViewCell()
			{
				View = layout
			};

			TableSection section = this.Contracts.Root[0];

			section.Insert(section.Count - 1, viewCell);

			return layout;
		}

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            this.BackButton_Clicked(this.BackButton, EventArgs.Empty);
            return true;
        }
	}
}