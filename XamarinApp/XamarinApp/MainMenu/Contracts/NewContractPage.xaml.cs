using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP.Contracts;

namespace XamarinApp.MainMenu.Contracts
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage : ContentPage, IBackButton
	{
		private readonly SortedDictionary<string, SortedDictionary<string, string>> contractTypesPerCategory;
		private readonly XmppConfiguration xmppConfiguration;
		private readonly Page owner;
		private readonly List<string> contractTypes = new List<string>();
		private Contract template = null;
		private string contractCategory = string.Empty;
		private string contractType = string.Empty;
		private string templateId = string.Empty;
		private string role = string.Empty;
		private string visibility = string.Empty;

		public NewContractPage(XmppConfiguration XmppConfiguration, Page Owner,
			SortedDictionary<string, SortedDictionary<string, string>> ContractTypesPerCategory)
		{
			this.xmppConfiguration = XmppConfiguration;
			this.owner = Owner;
			this.contractTypesPerCategory = ContractTypesPerCategory;
			this.BindingContext = this;
			InitializeComponent();
		}

		public NewContractPage(XmppConfiguration XmppConfiguration, Page Owner, Contract Template)
		{
			this.xmppConfiguration = XmppConfiguration;
			this.owner = Owner;
			this.contractTypesPerCategory = null;
			this.template = Template;
			this.BindingContext = this;
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
				List<string> Result = new List<string>();

				if (!(this.contractTypesPerCategory is null))
					Result.AddRange(this.contractTypesPerCategory.Keys);
				
				return Result;
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
						this.contractTypesPerCategory.TryGetValue(value, out SortedDictionary<string, string> IdsPerType))
					{
						this.contractTypes.AddRange(IdsPerType.Keys);
					}

					this.ContractTypePicker.IsEnabled = this.contractTypes.Count > 0;
					this.ContractTypePicker.Items.Clear();

					foreach (string ContractType in this.contractTypes)
						this.ContractTypePicker.Items.Add(ContractType);
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
						this.contractTypesPerCategory.TryGetValue(this.contractCategory, out SortedDictionary<string, string> IdsPerType) &&
						IdsPerType.TryGetValue(value, out string TemplateId))
					{
						this.templateId = TemplateId;
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
					this.RemoveRole(this.role, xmppConfiguration.LegalIdentity.Id);
					this.role = value;
					this.AddRole(this.role, xmppConfiguration.LegalIdentity.Id);
				}
			}
		}

		private void RemoveRole(string Role, string LegalId)
		{
			Label ToRemove = null;
			int State = 0;

			foreach (View View in this.Roles.Children)
			{
				switch (State)
				{
					case 0:
						if (View is Label Label && Label.StyleId == Role)
							State++;
						break;

					case 1:
						if (View is Button Button)
						{
							if (!(ToRemove is null))
							{
								this.Roles.Children.Remove(ToRemove);
								Button.IsEnabled = true;
							}
							return;
						}
						else if (View is Label Label2 && Label2.StyleId == LegalId)
							ToRemove = Label2;
						break;
				}
			}
		}

		private void AddRole(string Role, string LegalId)
		{
			int State = 0;
			int i = 0;
			int NrParts = 0;
			Role RoleObj = null;

			foreach (Role R in this.template.Roles)
			{
				if (R.Name == Role)
				{
					RoleObj = R;
					break;
				}
			}

			if (RoleObj is null)
				return;

			foreach (View View in this.Roles.Children)
			{
				switch (State)
				{
					case 0:
						if (View is Label Label && Label.StyleId == Role)
							State++;
						break;

					case 1:
						if (View is Button Button)
						{
							TapGestureRecognizer OpenLegalId = new TapGestureRecognizer();
							OpenLegalId.Tapped += this.ShowLegalId;

							Label = new Label()
							{
								Text = LegalId,
								StyleId = LegalId,
								FontFamily = "Courier New",
								HorizontalOptions = LayoutOptions.FillAndExpand,
								HorizontalTextAlignment = TextAlignment.Center,
								FontAttributes = FontAttributes.Bold
							};

							Label.GestureRecognizers.Add(OpenLegalId);

							this.Roles.Children.Insert(i, Label);
							NrParts++;

							if (NrParts >= RoleObj.MaxCount)
								Button.IsEnabled = false;

							return;
						}
						else if (View is Label Label2)
							NrParts++;
						break;
				}

				i++;
			}
		}

		private async void ShowLegalId(object Sender, EventArgs e)
		{
			try
			{
				if (Sender is Label Label && !string.IsNullOrEmpty(Label.StyleId))
					await App.OpenLegalIdentity(Label.StyleId, "For inclusion as part in a contract.");
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
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
				this.template = await App.Contracts.GetContractAsync(this.templateId);
				Device.BeginInvokeOnMainThread(this.PopulateTemplateForm);
			}
			catch (Exception ex)
			{
				this.template = null;
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
		}

		private void PopulateTemplateForm()
		{
			Contract Contract = this.template;
			if (Contract is null)
				return;

			this.PopulateHumanReadableText();

			this.Parameters.Children.Clear();
			this.Roles.Children.Clear();

			this.RolePicker.Items.Clear();
			this.RolePicker.IsEnabled = Contract.Roles.Length > 0;
			this.VisibilityPicker.IsEnabled = true;

			foreach (Role Role in Contract.Roles)
			{
				this.RolePicker.Items.Add(Role.Name);

				this.Roles.Children.Add(new Label()
				{
					Text = Role.Name,
					FontSize = 18,
					TextColor = Color.Navy,
					LineBreakMode = LineBreakMode.NoWrap,
					StyleId = Role.Name
				});

				Populate(this.Roles, Role.ToXamarinForms(Contract.DefaultLanguage, Contract));

				if (Role.MinCount > 0)
				{
					Button Button = new Button()
					{
						Text = "Add Part",
						StyleId = Role.Name
					};

					Button.Clicked += AddPartButton_Clicked;

					this.Roles.Children.Add(Button);
				}
			}

			this.Parameters.Children.Add(new Label()
			{
				Text = "Parameters",
				FontSize = 18,
				TextColor = Color.Navy,
				LineBreakMode = LineBreakMode.WordWrap
			});

			Entry Entry;

			foreach (Parameter Parameter in Contract.Parameters)
			{
				Populate(Parameters, Parameter.ToXamarinForms(Contract.DefaultLanguage, Contract));

				Parameters.Children.Add(Entry = new Entry()
				{
					StyleId = Parameter.Name,
					Text = FilterDefaultValues(Parameter.ObjectValue?.ToString()),
					HorizontalOptions = LayoutOptions.FillAndExpand,
				});

				Entry.TextChanged += Entry_TextChanged;
			}

			if (this.xmppConfiguration.UsePin)
			{
				this.PinLabel.IsVisible = true;
				this.PIN.IsVisible = true;
			}

			this.ProposeButton.IsEnabled = true;

			// Contract.ArchiveOptional;
			// Contract.ArchiveRequired;
			// Contract.Duration;
		}

		private void AddPartButton_Clicked(object sender, EventArgs e)
		{
			if (sender is Button Button)
				App.ShowPage(new AddPartPage(this, (Id) => this.AddRole(Button.StyleId, Id)), false);
		}

		private void PopulateHumanReadableText()
		{
			this.HumanReadableText.Children.Clear();

			if (!(this.template is null))
				Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));
		}

		private void Entry_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is Entry Entry)
			{
				foreach (Parameter P in this.template.Parameters)
				{
					if (P.Name == Entry.StyleId)
					{
						if (P is StringParameter SP)
							SP.Value = e.NewTextValue;
						else if (P is NumericalParameter NP)
						{
							if (double.TryParse(e.NewTextValue, out double d))
							{
								NP.Value = d;
								Entry.BackgroundColor = Color.Default;
							}
							else
							{
								Entry.BackgroundColor = Color.Salmon;
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

		internal static void Populate(StackLayout Layout, string Xaml)
		{
			StackLayout HumanReadable = new StackLayout().LoadFromXaml(Xaml);

			List<View> Children = new List<View>();
			Children.AddRange(HumanReadable.Children);

			foreach (View Element in Children)
				Layout.Children.Add(Element);
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
			List<Part> Parts = new List<Part>();
			Contract Created = null;
			string Role = string.Empty;
			int State = 0;
			int Nr = 0;
			int Min = 0;
			int Max = 0;

			try
			{
				foreach (View View in this.Roles.Children)
				{
					switch (State)
					{
						case 0:
							if (View is Label Label && !string.IsNullOrEmpty(Label.StyleId))
							{
								Role = Label.StyleId;
								State++;
								Nr = Min = Max = 0;

								foreach (Role R in this.template.Roles)
								{
									if (R.Name == Role)
									{
										Min = R.MinCount;
										Max = R.MaxCount;
										break;
									}
								}
							}
							break;

						case 1:
							if (View is Button)
							{
								if (Nr < Min)
								{
									await this.DisplayAlert("Error", "The contract requires at least " + Min.ToString() + " part(s) of role " + Role + ". Add more parts to the contract and try again.", "OK");
									return;
								}

								if (Nr > Min)
								{
									await this.DisplayAlert("Error", "The contract requires at most " + Max.ToString() + " part(s) of role " + Role + ". Remove some of the parts from the contract and try again.", "OK");
									return;
								}

								State--;
								Role = string.Empty;
							}
							else if (View is Label Label2 && !string.IsNullOrEmpty(Role))
							{
								Parts.Add(new Part()
								{
									Role = Role,
									LegalId = Label2.StyleId
								});

								Nr++;
							}
							break;
					}
				}

				foreach (View View in this.Parameters.Children)
				{
					if (View is Entry Entry)
					{
						if (Entry.BackgroundColor == Color.Salmon)
						{
							Entry.Focus();
							await this.DisplayAlert("Error", "Your contract contains errors. Fix these errors and try again.", "OK");
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
						await this.DisplayAlert("Error", "Select contract visibility first, and then try again.", "OK");
						return;
				}

				if (string.IsNullOrEmpty(this.role))
				{
					this.RolePicker.Focus();
					await this.DisplayAlert("Error", "Select contract role first, and then try again.", "OK");
					return;
				}

				if (this.xmppConfiguration.UsePin && this.xmppConfiguration.ComputePinHash(this.PIN.Text) != this.xmppConfiguration.PinHash)
				{
					await this.DisplayAlert("Error", "Invalid PIN.", "OK");
					return;
				}

				Created = await App.Contracts.CreateContractAsync(this.templateId, Parts.ToArray(), this.template.Parameters, 
					this.template.Visibility, ContractParts.ExplicitlyDefined, this.template.Duration, this.template.ArchiveRequired, 
					this.template.ArchiveOptional, null, null, false);

				Created = await App.Contracts.SignContractAsync(Created, this.role, false);
			}
			catch (Exception ex)
			{
				await this.DisplayAlert("Error", ex.Message, "OK");
			}
			finally
			{
				if (!(Created is null))
					App.ShowPage(new ViewContractPage(this.xmppConfiguration, this.owner, Created, false), true);
			}
		}

		private void BackButton_Clicked(object sender, EventArgs e)
		{
			App.ShowPage(this.owner, true);
		}

		public bool BackClicked()
		{
			this.BackButton_Clicked(this, new EventArgs());
			return true;
		}

	}
}