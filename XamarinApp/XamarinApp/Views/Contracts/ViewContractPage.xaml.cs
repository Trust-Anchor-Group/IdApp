using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Extensions;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
        private static readonly TimeSpan DownloadContractTimeout = TimeSpan.FromSeconds(10);
		private readonly TagProfile tagProfile;
		private readonly Contract contract;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;
        private readonly IContractOrchestratorService contractOrchestratorService;

		public ViewContractPage(Contract contract, bool readOnly)
		{
			this.tagProfile = DependencyService.Resolve<TagProfile>();
			this.contract = contract;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
			InitializeComponent();

			// General Information

			AddTag(this.GeneralInformation, "Created", contract.Created.ToString());

			if (contract.Updated != DateTime.MinValue)
				AddTag(this.GeneralInformation, "Updated", contract.Updated.ToString());

			AddTag(this.GeneralInformation, "State", contract.State.ToString());
			AddTag(this.GeneralInformation, "Visibility", contract.Visibility.ToString());
			AddTag(this.GeneralInformation, "Duration", contract.Duration.ToString());
			AddTag(this.GeneralInformation, "From", contract.From.ToString());
			AddTag(this.GeneralInformation, "To", contract.To.ToString());
			AddTag(this.GeneralInformation, "Archiving (optional)", contract.ArchiveOptional.ToString());
			AddTag(this.GeneralInformation, "Archiving (required)", contract.ArchiveRequired.ToString());
			AddTag(this.GeneralInformation, "Can act as template", contract.CanActAsTemplate.ToString());

			// QR Code

			byte[] png = QR.GenerateCodePng("iotsc:" + contract.ContractId, (int)this.QrCode.WidthRequest, (int)this.QrCode.HeightRequest);
			this.QrCode.Source = ImageSource.FromStream(() => new MemoryStream(png));
			this.QrCode.IsVisible = true;

			// Parts

			bool hasSigned = false;
			bool acceptsSignatures =
				(contract.State == ContractState.Approved || contract.State == ContractState.BeingSigned) &&
				(!contract.SignAfter.HasValue || contract.SignAfter.Value < DateTime.Now) &&
				(!contract.SignBefore.HasValue || contract.SignBefore.Value > DateTime.Now);
			Dictionary<string, int> nrSignatures = new Dictionary<string, int>();

			if (!(contract.ClientSignatures is null))
			{
				foreach (ClientSignature signature in contract.ClientSignatures)
				{
					if (signature.LegalId == this.tagProfile.LegalIdentity.Id)
						hasSigned = true;

					if (!nrSignatures.TryGetValue(signature.Role, out int count))
						count = 0;

					nrSignatures[signature.Role] = count + 1;
				}
			}

			if (contract.SignAfter.HasValue)
				AddTag(this.Parts, "Sign after", contract.SignAfter.Value.ToString());

			if (contract.SignBefore.HasValue)
				AddTag(this.Parts, "Sign before", contract.SignBefore.Value.ToString());

			AddTag(this.Parts, "Mode", contract.PartsMode.ToString());

			if (!(contract.Parts is null))
			{
				TapGestureRecognizer openLegalId = new TapGestureRecognizer();
				openLegalId.Tapped += this.ShowLegalId;

				foreach (Part part in contract.Parts)
				{
					AddTag(this.Parts, part.Role, part.LegalId, false, part.LegalId, openLegalId);

					if (!readOnly && acceptsSignatures && !hasSigned && part.LegalId == this.tagProfile.LegalIdentity.Id)
					{
						Button button;

						this.Parts.Add(new ViewCell()
						{
							View = button = new Button()
							{
								Text = "Sign as " + part.Role,
								StyleId = part.Role
							}
						});

						button.Clicked += SignButton_Clicked;
					}
				}
			}

			// Roles

			if (!(contract.Roles is null))
			{
				foreach (Role role in contract.Roles)
				{
					string html = role.ToHTML(contract.DefaultLanguage, contract);
					html = Waher.Content.Html.HtmlDocument.GetBody(html);

					AddTag(this.Roles, role.Name, html + CountString(role.MinCount, role.MaxCount), true, string.Empty, null);

					if (!readOnly && acceptsSignatures && !hasSigned && this.contract.PartsMode == ContractParts.Open &&
						(!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount))
					{
						Button button;

						this.Roles.Add(new ViewCell()
						{
							View = button = new Button()
							{
								Text = "Sign as " + role.Name,
								StyleId = role.Name
							}
						});

						button.Clicked += SignButton_Clicked;
					}
				}
			}

			// Parameters

			if (!(contract.Parameters is null))
			{
				foreach (Parameter parameter in contract.Parameters)
					AddTag(this.Parameters, parameter.Name, parameter.ObjectValue?.ToString());
			}

			// Human-readable text

			string xaml = contract.ToXamarinForms(contract.DefaultLanguage);
			StackLayout humanReadable = new StackLayout().LoadFromXaml(xaml);

			List<View> children = new List<View>();
			children.AddRange(humanReadable.Children);

			foreach (View element in children)
				this.HumanReadableText.Children.Add(element);

			// Machine-readable information

			AddTag(this.MachineReadableSection, "Contract ID", contract.ContractId.ToString());

			if (!string.IsNullOrEmpty(contract.TemplateId))
				AddTag(this.MachineReadableSection, "Template ID", contract.TemplateId);

			AddTag(this.MachineReadableSection, "Digest", Convert.ToBase64String(contract.ContentSchemaDigest));
			AddTag(this.MachineReadableSection, "Hash Function", contract.ContentSchemaHashFunction.ToString());
			AddTag(this.MachineReadableSection, "Local Name", contract.ForMachinesLocalName.ToString());
			AddTag(this.MachineReadableSection, "Namespace", contract.ForMachinesNamespace.ToString());

			// Client signatures

			if (!(contract.ClientSignatures is null))
			{
				TapGestureRecognizer openClientSignature = new TapGestureRecognizer();
				openClientSignature.Tapped += this.ShowClientSignature;

				foreach (ClientSignature signature in contract.ClientSignatures)
				{
					string sign = Convert.ToBase64String(signature.DigitalSignature);

					AddTag(this.ClientSignatures, signature.Role, signature.LegalId + ", " + signature.BareJid + ", " +
						signature.Timestamp.ToString() + ", " + sign, false, sign, openClientSignature);
				}
			}

			// Server signature

			if (!(contract.ServerSignature is null))
			{
				TapGestureRecognizer openServerSignature = new TapGestureRecognizer();
				openServerSignature.Tapped += this.ShowServerSignature;

				AddTag(this.ServerSignature, contract.Provider, contract.ServerSignature.Timestamp.ToString() + ", " +
					Convert.ToBase64String(contract.ServerSignature.DigitalSignature), false, contract.ContractId, openServerSignature);
			}

			// Buttons

			if (!readOnly && !contract.IsLegallyBinding(true))
			{
				Button button;

				this.ButtonSection.Insert(0, new ViewCell()
				{
					View = button = new Button()
					{
						Text = "Obsolete Contract"
					}
				});

				button.Clicked += ObsoleteButton_Clicked;

				this.ButtonSection.Insert(1, new ViewCell()
				{
					View = button = new Button()
					{
						Text = "Delete Contract"
					}
				});

				button.Clicked += DeleteButton_Clicked;
			}

			this.LoadPhotos();
		}

        private async void LoadPhotos()
        {
            if (!(this.contract.Attachments is null))
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection photoSection = new TableSection();
                this.TableView.Root.Insert(i++, photoSection);

                foreach (Attachment attachment in this.contract.Attachments.GetImageAttachments())
                {
                    ViewCell viewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> p = await this.contractsService.GetContractAttachmentAsync(attachment.Url, DownloadContractTimeout);

                        using (TemporaryFile file = p.Value)
                        {
                            MemoryStream ms = new MemoryStream();

                            file.Position = 0;
                            await file.CopyToAsync(ms);
                            ms.Position = 0;

                            viewCell = new ViewCell()
                            {
                                View = new Image()
                                {
                                    Source = ImageSource.FromStream(() => ms)
                                }
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        viewCell = new ViewCell()
                        {
                            View = new Label()
                            {
                                Text = ex.Message
                            }
                        };
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        photoSection.Add(viewCell);
                    });
                }
            }
        }

		private async void SignButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (sender is Button button && !string.IsNullOrEmpty(button.StyleId))
				{
					Contract contract = await this.contractsService.SignContractAsync(this.contract, button.StyleId, false);

					await this.navigationService.DisplayAlert("Message", "Contract successfully signed.");

					await this.navigationService.ReplaceAsync(new ViewContractPage(contract, false));
				}
			}
			catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void ShowLegalId(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
					await this.contractOrchestratorService.OpenLegalIdentity(layout.StyleId, "Reviewing contract where you are part.");
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void ShowClientSignature(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
				{
					string sign = layout.StyleId;

					foreach (ClientSignature signature in this.contract.ClientSignatures)
					{
						if (sign == Convert.ToBase64String(signature.DigitalSignature))
						{
							string legalId = signature.LegalId;
							LegalIdentity identity = await this.contractsService.GetLegalIdentityAsync(legalId);

							await this.navigationService.PushAsync(new ClientSignaturePage(signature, identity));
							return;
						}
					}
				}
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void ShowServerSignature(object sender, EventArgs e)
		{
			try
			{
				if (sender is StackLayout layout && !string.IsNullOrEmpty(layout.StyleId))
                    await this.navigationService.PushAsync(new ServerSignaturePage(this, this.contract));
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private static string CountString(int min, int max)
		{
			if (min == max)
			{
				if (max == 1)
					return string.Empty;
				else
					return " (" + max.ToString() + ")";
			}
			else
				return " (" + min.ToString() + "-" + max.ToString() + ")";
		}

		private static void AddTag(TableSection tags, string key, string value)
		{
			AddTag(tags, key, value, false, string.Empty, null);
		}

		private static void AddTag(TableSection tags, string key, string value, bool html, string styleId, TapGestureRecognizer tapGestureRecognizer)
		{
			StackLayout layout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				StyleId = styleId
			};

			ViewCell viewCell = new ViewCell()
			{
				View = layout
			};

			tags.Add(viewCell);

			layout.Children.Add(new Label()
			{
				Text = key + ":",
				LineBreakMode = LineBreakMode.NoWrap
			});

			layout.Children.Add(new Label()
			{
				Text = value,
				TextType = html ? TextType.Html : TextType.Text,
				FontAttributes = FontAttributes.Bold,
				LineBreakMode = LineBreakMode.WordWrap
			});

			if (!(tapGestureRecognizer is null))
				layout.GestureRecognizers.Add(tapGestureRecognizer);
		}

		internal void MoveInfo(TableView tableView)
		{
			TableRoot root = tableView.Root;

			root.Insert(root.Count - 1, this.GeneralInformation);
			root.Insert(root.Count - 1, this.QrCodeSection);
			root.Insert(root.Count - 1, this.Roles);
			root.Insert(root.Count - 1, this.Parts);
			root.Insert(root.Count - 1, this.Parameters);
			root.Insert(root.Count - 1, this.HumanReadableSection);
			root.Insert(root.Count - 1, this.MachineReadableSection);
			root.Insert(root.Count - 1, this.ClientSignatures);
			root.Insert(root.Count - 1, this.ServerSignature);
		}

		private async void ObsoleteButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				Contract contract = await this.contractsService.ObsoleteContractAsync(this.contract.ContractId);

                await this.navigationService.DisplayAlert("Message", "Contract has been obsoleted.");

                await this.navigationService.PushAsync(new ViewContractPage(contract, false));
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private async void DeleteButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				Contract contract = await this.contractsService.DeleteContractAsync(this.contract.ContractId);

				await this.navigationService.DisplayAlert("Message", "Contract has been deleted.");

                await this.navigationService.PushAsync(new ViewContractPage(contract, false));
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}