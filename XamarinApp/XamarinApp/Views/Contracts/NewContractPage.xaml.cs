using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.Networking.XMPP.Contracts;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage
	{
		private readonly SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory;
		private readonly TagProfile tagProfile;
		private readonly List<string> contractTypes = new List<string>();
		private Contract template = null;
		private string contractCategory = string.Empty;
		private string contractType = string.Empty;
		private string templateId = string.Empty;
		private string role = string.Empty;
		private string visibility = string.Empty;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;
        private readonly IIdentityOrchestratorService identityOrchestratorService;

		public NewContractPage(SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory)
		{
			this.tagProfile = DependencyService.Resolve<TagProfile>();
			this.contractTypesPerCategory = contractTypesPerCategory;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.identityOrchestratorService = DependencyService.Resolve<IIdentityOrchestratorService>();
			InitializeComponent();
		}

		public NewContractPage(Contract template)
		{
            this.tagProfile = DependencyService.Resolve<TagProfile>();
			this.contractTypesPerCategory = null;
			this.template = template;
			this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
			InitializeComponent();

			this.ContractCategoryLabel.IsVisible = false;
			this.ContractCategoryPicker.IsVisible = false;
			this.ContractTypeLabel.IsVisible = false;
			this.ContractTypePicker.IsVisible = false;

			this.PopulateTemplateForm();
		}

		public IList<string> ContractCategories
		{
			get
			{
				List<string> result = new List<string>();

				if (!(this.contractTypesPerCategory is null))
					result.AddRange(this.contractTypesPerCategory.Keys);
				
				return result;
			}
		}

		public string ContractCategory
		{
			get => this.contractCategory;
			set
			{
				if (this.contractCategory != value)
				{
					this.contractCategory = value;
					this.contractTypes.Clear();

					if (!(this.contractTypesPerCategory is null) &&
						this.contractTypesPerCategory.TryGetValue(value, out SortedDictionary<string, string> idsPerType))
					{
						this.contractTypes.AddRange(idsPerType.Keys);
					}

					this.ContractTypePicker.IsEnabled = this.contractTypes.Count > 0;
					this.ContractTypePicker.Items.Clear();

					foreach (string contractType in this.contractTypes)
						this.ContractTypePicker.Items.Add(contractType);
				}
			}
		}

		public string ContractType
		{
			get => this.contractType;
			set
			{
				if (this.contractType != value)
				{
					this.contractType = value;
					this.ClearTemplate();

					if (!(value is null) &&
						!(this.contractTypesPerCategory is null) &&
						this.contractTypesPerCategory.TryGetValue(this.contractCategory, out SortedDictionary<string, string> idsPerType) &&
						idsPerType.TryGetValue(value, out string templateId))
					{
						this.templateId = templateId;
						this.LoadTemplate();
					}
				}
			}
		}

		public string Role
		{
			get => this.role;
			set
			{
				if (this.role != value)
				{
					this.RemoveRole(this.role, tagProfile.LegalIdentity.Id);
					this.role = value;
					this.AddRole(this.role, tagProfile.LegalIdentity.Id);
				}
			}
		}

		private void RemoveRole(string role, string legalId)
		{
			Label toRemove = null;
			int state = 0;

			foreach (View view in this.Roles.Children)
			{
				switch (state)
				{
					case 0:
						if (view is Label label && label.StyleId == role)
							state++;
						break;

					case 1:
						if (view is Button button)
						{
							if (!(toRemove is null))
							{
								this.Roles.Children.Remove(toRemove);
								button.IsEnabled = true;
							}
							return;
						}
						else if (view is Label label2 && label2.StyleId == legalId)
							toRemove = label2;
						break;
				}
			}
		}

		private void AddRole(string role, string legalId)
		{
			int state = 0;
			int i = 0;
			int nrParts = 0;
			Role roleObj = null;

			foreach (Role r in this.template.Roles)
			{
				if (r.Name == role)
				{
					roleObj = r;
					break;
				}
			}

			if (roleObj is null)
				return;

			foreach (View view in this.Roles.Children)
			{
				switch (state)
				{
					case 0:
						if (view is Label label && label.StyleId == role)
							state++;
						break;

					case 1:
						if (view is Button button)
						{
							TapGestureRecognizer openLegalId = new TapGestureRecognizer();
							openLegalId.Tapped += this.ShowLegalId;

							label = new Label()
							{
								Text = legalId,
								StyleId = legalId,
								FontFamily = "Courier New",
								HorizontalOptions = LayoutOptions.FillAndExpand,
								HorizontalTextAlignment = TextAlignment.Center,
								FontAttributes = FontAttributes.Bold
							};

							label.GestureRecognizers.Add(openLegalId);

							this.Roles.Children.Insert(i, label);
							nrParts++;

							if (nrParts >= roleObj.MaxCount)
								button.IsEnabled = false;

							return;
						}
						else if (view is Label label2)
							nrParts++;
						break;
				}

				i++;
			}
		}

		private async void ShowLegalId(object sender, EventArgs e)
		{
			try
			{
				if (sender is Label label && !string.IsNullOrEmpty(label.StyleId))
					await this.identityOrchestratorService.OpenLegalIdentity(label.StyleId, "For inclusion as part in a contract.");
			}
			catch (Exception ex)
			{
				await this.navigationService.DisplayAlert(ex);
			}
		}

		public string Visibility
		{
			get => this.visibility;
			set => this.visibility = value;
		}

		private async void LoadTemplate()
		{
			try
			{
				this.template = await this.contractsService.GetContractAsync(this.templateId);
				Device.BeginInvokeOnMainThread(this.PopulateTemplateForm);
			}
			catch (Exception ex)
			{
				this.template = null;
                await this.navigationService.DisplayAlert(ex);
			}
		}

		private void PopulateTemplateForm()
		{
			Contract contract = this.template;
			if (contract is null)
				return;

			this.PopulateHumanReadableText();

			this.Parameters.Children.Clear();
			this.Roles.Children.Clear();

			this.RolePicker.Items.Clear();
			this.RolePicker.IsEnabled = contract.Roles.Length > 0;
			this.VisibilityPicker.IsEnabled = true;

			foreach (Role role in contract.Roles)
			{
				this.RolePicker.Items.Add(role.Name);

				this.Roles.Children.Add(new Label()
				{
					Text = role.Name,
					FontSize = 18,
					TextColor = Color.Navy,
					LineBreakMode = LineBreakMode.NoWrap,
					StyleId = role.Name
				});

				Populate(this.Roles, role.ToXamarinForms(contract.DefaultLanguage, contract));

				if (role.MinCount > 0)
				{
					Button button = new Button()
					{
						Text = "Add Part",
						StyleId = role.Name
					};

					button.Clicked += AddPartButton_Clicked;

					this.Roles.Children.Add(button);
				}
			}

			this.Parameters.Children.Add(new Label()
			{
				Text = "Parameters",
				FontSize = 18,
				TextColor = Color.Navy,
				LineBreakMode = LineBreakMode.WordWrap
			});

			Entry entry;

			foreach (Parameter parameter in contract.Parameters)
			{
				Populate(Parameters, parameter.ToXamarinForms(contract.DefaultLanguage, contract));

				Parameters.Children.Add(entry = new Entry()
				{
					StyleId = parameter.Name,
					Text = FilterDefaultValues(parameter.ObjectValue?.ToString()),
					HorizontalOptions = LayoutOptions.FillAndExpand,
				});

				entry.TextChanged += Entry_TextChanged;
			}

			if (this.tagProfile.UsePin)
			{
				this.PinLabel.IsVisible = true;
				this.PIN.IsVisible = true;
			}

			this.ProposeButton.IsEnabled = true;

			// Contract.ArchiveOptional;
			// Contract.ArchiveRequired;
			// Contract.Duration;
		}

		private async void AddPartButton_Clicked(object sender, EventArgs e)
		{
			if (sender is Button button)
            {
                ScanQrCodePage page = new ScanQrCodePage();
                page.Open += ScanQrPage_Open;
                await this.navigationService.PushAsync(page);
                string code = await this.OpenQrCode();
                await this.navigationService.PopAsync();
                page.Open -= ScanQrPage_Open;

                string id = !string.IsNullOrWhiteSpace(code) && code.StartsWith(Constants.IoTSchemes.IotId + ":") ? code.Substring(6) : string.Empty;

                this.AddRole(button.StyleId, id);
            }
		}

        TaskCompletionSource<string> openQrCode;

        private Task<string> OpenQrCode()
        {
            openQrCode = new TaskCompletionSource<string>();
            return openQrCode.Task;
        }

        private void ScanQrPage_Open(object sender, OpenEventArgs e)
        {
            if (openQrCode != null)
            {
                openQrCode.TrySetResult(e.Code);
                openQrCode = null;
            }
        }

		private void PopulateHumanReadableText()
		{
			this.HumanReadableText.Children.Clear();

			if (!(this.template is null))
				Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));
		}

		private void Entry_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is Entry entry)
			{
				foreach (Parameter p in this.template.Parameters)
				{
					if (p.Name == entry.StyleId)
					{
						if (p is StringParameter sp)
							sp.Value = e.NewTextValue;
						else if (p is NumericalParameter np)
						{
							if (double.TryParse(e.NewTextValue, out double d))
							{
								np.Value = d;
								entry.BackgroundColor = Color.Default;
							}
							else
							{
								entry.BackgroundColor = Color.Salmon;
								return;
							}
						}

						PopulateHumanReadableText();
						break;
					}
				}
			}
		}

		internal static string FilterDefaultValues(string s)
		{
			foreach (char ch in s)
			{
				if (char.ToUpper(ch) != ch)
					return s;
			}

			return string.Empty;
		}

		internal static void Populate(StackLayout layout, string xaml)
		{
			StackLayout humanReadable = new StackLayout().LoadFromXaml(xaml);

			List<View> children = new List<View>();
			children.AddRange(humanReadable.Children);

			foreach (View element in children)
				layout.Children.Add(element);
		}

		private void ClearTemplate()
		{
			this.Parameters.Children.Clear();
			this.Roles.Children.Clear();
			this.HumanReadableText.Children.Clear();

			this.ProposeButton.IsEnabled = false;
			this.RolePicker.IsEnabled = false;
			this.VisibilityPicker.IsEnabled = false;
			this.PinLabel.IsVisible = false;
			this.PIN.IsVisible = false;
		}

		private async void ProposeButton_Clicked(object sender, EventArgs e)
		{
			List<Part> parts = new List<Part>();
			Contract created = null;
			string role = string.Empty;
			int state = 0;
			int nr = 0;
			int min = 0;
			int max = 0;

			try
			{
				foreach (View view in this.Roles.Children)
				{
					switch (state)
					{
						case 0:
							if (view is Label label && !string.IsNullOrEmpty(label.StyleId))
							{
								role = label.StyleId;
								state++;
								nr = min = max = 0;

								foreach (Role r in this.template.Roles)
								{
									if (r.Name == role)
									{
										min = r.MinCount;
										max = r.MaxCount;
										break;
									}
								}
							}
							break;

						case 1:
							if (view is Button)
							{
								if (nr < min)
								{
									await this.navigationService.DisplayAlert("Error", "The contract requires at least " + min.ToString() + " part(s) of role " + role + ". Add more parts to the contract and try again.");
									return;
								}

								if (nr > min)
								{
                                    await this.navigationService.DisplayAlert("Error", "The contract requires at most " + max.ToString() + " part(s) of role " + role + ". Remove some of the parts from the contract and try again.");
									return;
								}

								state--;
								role = string.Empty;
							}
							else if (view is Label label2 && !string.IsNullOrEmpty(role))
							{
								parts.Add(new Part()
								{
									Role = role,
									LegalId = label2.StyleId
								});

								nr++;
							}
							break;
					}
				}

				foreach (View view in this.Parameters.Children)
				{
					if (view is Entry entry)
					{
						if (entry.BackgroundColor == Color.Salmon)
						{
							entry.Focus();
                            await this.navigationService.DisplayAlert("Error", "Your contract contains errors. Fix these errors and try again.");
							return;
						}
					}
				}

				this.template.PartsMode = ContractParts.Open;

				switch (this.VisibilityPicker.Items.IndexOf(this.visibility))
				{
					case 0:
						this.template.Visibility = ContractVisibility.CreatorAndParts;
						break;

					case 1:
						this.template.Visibility = ContractVisibility.DomainAndParts;
						break;

					case 2:
						this.template.Visibility = ContractVisibility.Public;
						break;

					case 3:
						this.template.Visibility = ContractVisibility.PublicSearchable;
						break;

					default:
						this.VisibilityPicker.Focus();
                        await this.navigationService.DisplayAlert("Error", "Select contract visibility first, and then try again.");
						return;
				}

				if (string.IsNullOrEmpty(this.role))
				{
					this.RolePicker.Focus();
                    await this.navigationService.DisplayAlert("Error", "Select contract role first, and then try again.");
					return;
				}

				if (this.tagProfile.UsePin && this.tagProfile.ComputePinHash(this.PIN.Text) != this.tagProfile.PinHash)
				{
                    await this.navigationService.DisplayAlert("Error", "Invalid PIN.");
					return;
				}

				created = await this.contractsService.CreateContractAsync(this.templateId, parts.ToArray(), this.template.Parameters, 
					this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration, this.template.ArchiveRequired, 
					this.template.ArchiveOptional, null, null, false);

				created = await this.contractsService.SignContractAsync(created, this.role, false);
			}
			catch (Exception ex)
			{
                await this.navigationService.DisplayAlert(ex);
			}
			finally
			{
				if (!(created is null))
					await this.navigationService.PushAsync(new ViewContractPage(created, false));
			}
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
	}
}