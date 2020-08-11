using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.MainMenu.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MyContractsPage : ContentPage, IBackButton
	{
		private readonly Dictionary<string, Contract> contracts = new Dictionary<string, Contract>();
		private readonly XmppConfiguration xmppConfiguration;
		private readonly Page owner;
		private readonly bool createdContracts;

		public MyContractsPage(XmppConfiguration XmppConfiguration, Page Owner, bool CreatedContracts)
		{
			this.xmppConfiguration = XmppConfiguration;
			this.owner = Owner;
			this.createdContracts = CreatedContracts;
			this.BindingContext = this;
			InitializeComponent();

			this.LoadMyContracts();
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		private async void LoadMyContracts()
		{
			try
			{
				string[] ContractIds;

				if (this.createdContracts)
					ContractIds = await App.Contracts.GetCreatedContractsAsync();
				else
					ContractIds = await App.Contracts.GetSignedContractsAsync();

				TapGestureRecognizer TapGestureRecognizer = new TapGestureRecognizer();
				TapGestureRecognizer.Tapped += (sender, e) =>
				{
					if (sender is StackLayout Layout && 
						!string.IsNullOrEmpty(Layout.StyleId) &&
						this.contracts.TryGetValue(Layout.StyleId, out Contract Contract))
					{
						App.ShowPage(new ViewContractPage(this.xmppConfiguration, this, Contract, false), false);
					}
				};

				if (ContractIds.Length == 0)
				{
					await Device.InvokeOnMainThreadAsync(() =>
					{
						StackLayout Layout = this.AddCell();

						Layout.Children.Add(new Label()
						{
							Text = "No contracts found."
						});
					});
				}
				else
				{
					foreach (string ContractId in ContractIds)
					{
						Contract Contract = await App.Contracts.GetContractAsync(ContractId);
						this.contracts[ContractId] = Contract;

						await Device.InvokeOnMainThreadAsync(() =>
						{
							StackLayout Layout = this.AddCell();
							Layout.StyleId = ContractId;

							Layout.Children.Add(new Label()
							{
								Text = Contract.Created.ToString() + ":",
								LineBreakMode = LineBreakMode.NoWrap
							});

							Layout.Children.Add(new Label()
							{
								Text = Contract.ContractId + " (" + Contract.ForMachinesNamespace + "#" + Contract.ForMachinesLocalName + ")",
								TextType = TextType.Text,
								FontAttributes = FontAttributes.Bold,
								LineBreakMode = LineBreakMode.CharacterWrap
							});

							Layout.GestureRecognizers.Add(TapGestureRecognizer);
						});
					}
				}
			}
			catch (Exception ex)
			{
				await Device.InvokeOnMainThreadAsync(() =>
				{
					StackLayout Layout = this.AddCell();

					Layout.Children.Add(new Label()
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
			StackLayout Layout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal
			};

			ViewCell ViewCell = new ViewCell()
			{
				View = Layout
			};

			TableSection Section = this.Contracts.Root[0];

			Section.Insert(Section.Count - 1, ViewCell);

			return Layout;
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}