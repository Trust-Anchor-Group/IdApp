using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage : ContentPage, IBackButton
    {
        private readonly ITagService tagService;
		private readonly Page owner;
		private readonly Contract contract;

		public ViewContractPage(Page Owner, Contract Contract, bool ReadOnly)
		{
            InitializeComponent();
            this.tagService = DependencyService.Resolve<ITagService>();
			this.owner = Owner;
			this.contract = Contract;
			this.BindingContext = this;

			// General Information

			AddTag(this.GeneralInformation, "Created", Contract.Created.ToString());

			if (Contract.Updated != DateTime.MinValue)
				AddTag(this.GeneralInformation, "Updated", Contract.Updated.ToString());

			AddTag(this.GeneralInformation, "State", Contract.State.ToString());
			AddTag(this.GeneralInformation, "Visibility", Contract.Visibility.ToString());
			AddTag(this.GeneralInformation, "Duration", Contract.Duration.ToString());
			AddTag(this.GeneralInformation, "From", Contract.From.ToString());
			AddTag(this.GeneralInformation, "To", Contract.To.ToString());
			AddTag(this.GeneralInformation, "Archiving (optional)", Contract.ArchiveOptional.ToString());
			AddTag(this.GeneralInformation, "Archiving (required)", Contract.ArchiveRequired.ToString());
			AddTag(this.GeneralInformation, "Can act as template", Contract.CanActAsTemplate.ToString());

			// QR Code

			byte[] Png = QR.GenerateCodePng("iotsc:" + Contract.ContractId, (int)this.QrCode.WidthRequest, (int)this.QrCode.HeightRequest);
			this.QrCode.Source = ImageSource.FromStream(() => new MemoryStream(Png));
			this.QrCode.IsVisible = true;

			// Parts

			bool HasSigned = false;
			bool AcceptsSignatures =
				(Contract.State == ContractState.Approved || Contract.State == ContractState.BeingSigned) &&
				(!Contract.SignAfter.HasValue || Contract.SignAfter.Value < DateTime.Now) &&
				(!Contract.SignBefore.HasValue || Contract.SignBefore.Value > DateTime.Now);
			Dictionary<string, int> NrSignatures = new Dictionary<string, int>();

			if (!(Contract.ClientSignatures is null))
			{
				foreach (ClientSignature Signature in Contract.ClientSignatures)
				{
					if (Signature.LegalId == this.tagService.Configuration.LegalIdentity.Id)
						HasSigned = true;

					if (!NrSignatures.TryGetValue(Signature.Role, out int Count))
						Count = 0;

					NrSignatures[Signature.Role] = Count + 1;
				}
			}

			if (Contract.SignAfter.HasValue)
				AddTag(this.Parts, "Sign after", Contract.SignAfter.Value.ToString());

			if (Contract.SignBefore.HasValue)
				AddTag(this.Parts, "Sign before", Contract.SignBefore.Value.ToString());

			AddTag(this.Parts, "Mode", Contract.PartsMode.ToString());

			if (!(Contract.Parts is null))
			{
				TapGestureRecognizer OpenLegalId = new TapGestureRecognizer();
				OpenLegalId.Tapped += this.ShowLegalId;

				foreach (Part Part in Contract.Parts)
				{
					AddTag(this.Parts, Part.Role, Part.LegalId, false, Part.LegalId, OpenLegalId);

					if (!ReadOnly && AcceptsSignatures && !HasSigned && Part.LegalId == this.tagService.Configuration.LegalIdentity.Id)
					{
						Button Button;

						this.Parts.Add(new ViewCell()
						{
							View = Button = new Button()
							{
								Text = "Sign as " + Part.Role,
								StyleId = Part.Role
							}
						});

						Button.Clicked += SignButton_Clicked;
					}
				}
			}

			// Roles

			if (!(Contract.Roles is null))
			{
				foreach (Role Role in Contract.Roles)
				{
					string Html = Role.ToHTML(Contract.DefaultLanguage, Contract);
					Html = Waher.Content.Html.HtmlDocument.GetBody(Html);

					AddTag(this.Roles, Role.Name, Html + CountString(Role.MinCount, Role.MaxCount), true, string.Empty, null);

					if (!ReadOnly && AcceptsSignatures && !HasSigned && this.contract.PartsMode == ContractParts.Open &&
						(!NrSignatures.TryGetValue(Role.Name, out int Count) || Count < Role.MaxCount))
					{
						Button Button;

						this.Roles.Add(new ViewCell()
						{
							View = Button = new Button()
							{
								Text = "Sign as " + Role.Name,
								StyleId = Role.Name
							}
						});

						Button.Clicked += SignButton_Clicked;
					}
				}
			}

			// Parameters

			if (!(Contract.Parameters is null))
			{
				foreach (Parameter Parameter in Contract.Parameters)
					AddTag(this.Parameters, Parameter.Name, Parameter.ObjectValue?.ToString());
			}

			// Human-readable text

			string Xaml = Contract.ToXamarinForms(Contract.DefaultLanguage);
			StackLayout HumanReadable = new StackLayout().LoadFromXaml(Xaml);

			List<View> Children = new List<View>();
			Children.AddRange(HumanReadable.Children);

			foreach (View Element in Children)
				this.HumanReadableText.Children.Add(Element);

			// Machine-readable information

			AddTag(this.MachineReadableSection, "Contract ID", Contract.ContractId.ToString());

			if (!string.IsNullOrEmpty(Contract.TemplateId))
				AddTag(this.MachineReadableSection, "Template ID", Contract.TemplateId);

			AddTag(this.MachineReadableSection, "Digest", Convert.ToBase64String(Contract.ContentSchemaDigest));
			AddTag(this.MachineReadableSection, "Hash Function", Contract.ContentSchemaHashFunction.ToString());
			AddTag(this.MachineReadableSection, "Local Name", Contract.ForMachinesLocalName.ToString());
			AddTag(this.MachineReadableSection, "Namespace", Contract.ForMachinesNamespace.ToString());

			// Client signatures

			if (!(Contract.ClientSignatures is null))
			{
				TapGestureRecognizer OpenClientSignature = new TapGestureRecognizer();
				OpenClientSignature.Tapped += this.ShowClientSignature;

				foreach (ClientSignature Signature in Contract.ClientSignatures)
				{
					string Sign = Convert.ToBase64String(Signature.DigitalSignature);

					AddTag(this.ClientSignatures, Signature.Role, Signature.LegalId + ", " + Signature.BareJid + ", " +
						Signature.Timestamp.ToString() + ", " + Sign, false, Sign, OpenClientSignature);
				}
			}

			// Server signature

			if (!(Contract.ServerSignature is null))
			{
				TapGestureRecognizer OpenServerSignature = new TapGestureRecognizer();
				OpenServerSignature.Tapped += this.ShowServerSignature;

				AddTag(this.ServerSignature, Contract.Provider, Contract.ServerSignature.Timestamp.ToString() + ", " +
					Convert.ToBase64String(Contract.ServerSignature.DigitalSignature), false, Contract.ContractId, OpenServerSignature);
			}

			// Buttons

			if (!ReadOnly && !Contract.IsLegallyBinding(true))
			{
				Button Button;

				this.ButtonSection.Insert(0, new ViewCell()
				{
					View = Button = new Button()
					{
						Text = "Obsolete Contract"
					}
				});

				Button.Clicked += ObsoleteButton_Clicked;

				this.ButtonSection.Insert(1, new ViewCell()
				{
					View = Button = new Button()
					{
						Text = "Delete Contract"
					}
				});

				Button.Clicked += DeleteButton_Clicked;
			}

            this.LoadPhotos();
        }

        private async void LoadPhotos()
        {
            if (!(this.contract.Attachments is null))
            {
                int i = this.TableView.Root.IndexOf(this.ButtonSection);
                TableSection PhotoSection = new TableSection();
                this.TableView.Root.Insert(i++, PhotoSection);

                foreach (Attachment Attachment in this.contract.Attachments.Where(x => x.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
                {
                    ViewCell ViewCell;

                    try
                    {
                        KeyValuePair<string, TemporaryFile> P = await this.tagService.GetAttachmentAsync(Attachment.Url, TimeSpan.FromSeconds(10));

                        using (TemporaryFile File = P.Value)
                        {
                            MemoryStream ms = new MemoryStream();

                            File.Position = 0;
                            await File.CopyToAsync(ms);
                            ms.Position = 0;

                            ViewCell = new ViewCell()
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
                        ViewCell = new ViewCell()
                        {
                            View = new Label()
                            {
                                Text = ex.Message
                            }
                        };
                    }

                    await Device.InvokeOnMainThreadAsync(() =>
                    {
                        PhotoSection.Add(ViewCell);
                    });
                }
            }
        }

        private async void SignButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				if (sender is Button Button && !string.IsNullOrEmpty(Button.StyleId))
				{
					Contract Contract = await this.tagService.Contracts.SignContractAsync(this.contract, Button.StyleId, false);

					await this.DisplayAlert("Message", "Contract successfully signed.", "OK");

					App.ShowPage(new ViewContractPage(this.owner, Contract, false), true);
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private async void ShowLegalId(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					await App.OpenLegalIdentity(Layout.StyleId, "Reviewing contract where you are part.");
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private async void ShowClientSignature(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
				{
					string Sign = Layout.StyleId;

					foreach (ClientSignature Signature in this.contract.ClientSignatures)
					{
						if (Sign == Convert.ToBase64String(Signature.DigitalSignature))
						{
							string LegalId = Signature.LegalId;
							LegalIdentity Identity = await this.tagService.Contracts.GetLegalIdentityAsync(LegalId);

							App.ShowPage(new ClientSignaturePage(this, Signature, Identity), false);
							return;
						}
					}
				}
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private async void ShowServerSignature(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is StackLayout Layout && !string.IsNullOrEmpty(Layout.StyleId))
					App.ShowPage(new ServerSignaturePage(this, this.contract), false);
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private static string CountString(int Min, int Max)
		{
			if (Min == Max)
			{
				if (Max == 1)
					return string.Empty;
				else
					return " (" + Max.ToString() + ")";
			}
			else
				return " (" + Min.ToString() + "-" + Max.ToString() + ")";
		}

		private static void AddTag(TableSection Tags, string Key, string Value)
		{
			AddTag(Tags, Key, Value, false, string.Empty, null);
		}

		private static void AddTag(TableSection Tags, string Key, string Value, bool Html, string StyleId, TapGestureRecognizer TapGestureRecognizer)
		{
			StackLayout Layout = new StackLayout()
			{
				Orientation = StackOrientation.Horizontal,
				StyleId = StyleId
			};

			ViewCell ViewCell = new ViewCell()
			{
				View = Layout
			};

			Tags.Add(ViewCell);

			Layout.Children.Add(new Label()
			{
				Text = Key + ":",
				LineBreakMode = LineBreakMode.NoWrap
			});

			Layout.Children.Add(new Label()
			{
				Text = Value,
				TextType = Html ? TextType.Html : TextType.Text,
				FontAttributes = FontAttributes.Bold,
				LineBreakMode = LineBreakMode.WordWrap
			});

			if (!(TapGestureRecognizer is null))
				Layout.GestureRecognizers.Add(TapGestureRecognizer);
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		internal void MoveInfo(TableView TableView)
		{
			TableRoot Root = TableView.Root;

			Root.Insert(Root.Count - 1, this.GeneralInformation);
			Root.Insert(Root.Count - 1, this.QrCodeSection);
			Root.Insert(Root.Count - 1, this.Roles);
			Root.Insert(Root.Count - 1, this.Parts);
			Root.Insert(Root.Count - 1, this.Parameters);
			Root.Insert(Root.Count - 1, this.HumanReadableSection);
			Root.Insert(Root.Count - 1, this.MachineReadableSection);
			Root.Insert(Root.Count - 1, this.ClientSignatures);
			Root.Insert(Root.Count - 1, this.ServerSignature);
		}

		private async void ObsoleteButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				Contract Contract = await this.tagService.Contracts.ObsoleteContractAsync(this.contract.ContractId);

				await this.DisplayAlert("Message", "Contract has been obsoleted.", "OK");

				App.ShowPage(new ViewContractPage(this.owner, Contract, false), true);
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private async void DeleteButton_Clicked(object sender, EventArgs e)
		{
			try
			{
				Contract Contract = await this.tagService.Contracts.DeleteContractAsync(this.contract.ContractId);

				await this.DisplayAlert("Message", "Contract has been deleted.", "OK");

				App.ShowPage(new ViewContractPage(this.owner, Contract, false), true);
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}