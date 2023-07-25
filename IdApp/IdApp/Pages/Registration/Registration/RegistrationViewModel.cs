using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace IdApp.Pages.Registration.Registration
{
	/// <summary>
	/// The view model to bind to for displaying a registration page or view to the user.
	/// </summary>
	public class RegistrationViewModel : BaseViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
		/// </summary>
		public RegistrationViewModel()
		{
			this.GoToPrevCommand = new Command(() => this.GoToPrev(), () => (RegistrationStep)this.CurrentStep > RegistrationStep.ValidateContactInfo);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
		/// </summary>
		public void SetPagesContainer(List<RegistrationStepView> Items)
		{
			this.RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>();

			foreach (RegistrationStepView Item in Items)
			{
				this.RegistrationSteps.Add(this.AddChildViewModel((RegistrationStepViewModel)Item.BindingContext));
			};

			this.CurrentStep = (int)this.TagProfile.Step;
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.RegistrationSteps.ForEach(x => x.StepCompleted += this.RegistrationStep_Completed);
			await this.SyncTagProfileStep();
		}

		/// <inheritdoc />
		protected override Task OnDispose()
		{
			this.RegistrationSteps.ForEach(x => x.StepCompleted -= this.RegistrationStep_Completed);

			return base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="CurrentState"/>
		/// </summary>
		public static readonly BindableProperty CurrentStateProperty =
			BindableProperty.Create(nameof(CurrentState), typeof(LayoutState), typeof(RegistrationViewModel), LayoutState.Custom);

		/// <summary>
		/// </summary>
		public LayoutState CurrentState
		{
			get => (LayoutState)this.GetValue(CurrentStateProperty);
			set => this.SetValue(CurrentStateProperty, value);
		}

		/// <summary>
		/// See <see cref="CustomState"/>
		/// </summary>
		public static readonly BindableProperty CustomStateProperty =
			BindableProperty.Create(nameof(CustomState), typeof(string), typeof(RegistrationViewModel), string.Empty);

		/// <summary>
		/// </summary>
		public string CustomState
		{
			get => (string)this.GetValue(CustomStateProperty);
			set => this.SetValue(CustomStateProperty, value);
		}

		/// <summary>
		/// The list of steps needed to register a digital identity.
		/// </summary>
		public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; private set; }

		/// <summary>
		/// The command to bind to for moving backwards to the previous step in the registration process.
		/// </summary>
		public ICommand GoToPrevCommand { get; }

		/// <summary>
		/// See <see cref="CanGoBack"/>
		/// </summary>
		public static readonly BindableProperty CanGoBackProperty =
			BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(RegistrationViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether navigation back to the previous registration step can be performed.
		/// </summary>
		public bool CanGoBack
		{
			get => (bool)this.GetValue(CanGoBackProperty);
			set => this.SetValue(CanGoBackProperty, value);
		}

		/// <summary>
		/// See <see cref="CurrentStep"/>
		/// </summary>
		public static readonly BindableProperty CurrentStepProperty =
			BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(RegistrationViewModel), -1, propertyChanged: OnCurrentStepPropertyChanged);

		static void OnCurrentStepPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((RegistrationViewModel)bindable).OnCurrentStepPropertyChanged();

		async void OnCurrentStepPropertyChanged()
		{
			this.UpdateStepVariables();

			await this.RegistrationSteps[this.CurrentStep].DoAssignProperties();
		}


		/// <summary>
		/// Gets or sets the current step from the list of <see cref="RegistrationSteps"/>.
		/// </summary>
		public int CurrentStep
		{
			get => (int)this.GetValue(CurrentStepProperty);
			set => this.SetValue(CurrentStepProperty, value);
		}

		/// <summary>
		/// See <see cref="CurrentStep"/>
		/// </summary>
		public static readonly BindableProperty CurrentStepTitleProperty =
			BindableProperty.Create(nameof(CurrentStepTitle), typeof(string), typeof(RegistrationViewModel), default(string));

		/// <summary>
		/// The title of the current step. Displayed in the UI.
		/// </summary>
		public string CurrentStepTitle
		{
			get => (string)this.GetValue(CurrentStepTitleProperty);
			set => this.SetValue(CurrentStepTitleProperty, value);
		}

		#endregion

		private void UpdateStepVariables()
		{
			this.CanGoBack = this.GoToPrevCommand.CanExecute(null);
			this.CurrentStepTitle = this.RegistrationSteps[this.CurrentStep].Title;

			switch ((RegistrationStep)this.CurrentStep)
			{
				case RegistrationStep.ValidateContactInfo:
					this.CustomState = "ValidateContactInfo";
					break;

				case RegistrationStep.Account:
					this.CustomState = "ChooseAccount";
					break;

				case RegistrationStep.RegisterIdentity:
					this.CustomState = "RegisterIdentity";
					break;

				case RegistrationStep.ValidateIdentity:
					this.CustomState = "ValidateIdentity";
					break;

				case RegistrationStep.Pin:
					this.CustomState = "DefinePin";
					break;
			}
		}

		/// <summary>
		/// An event handler for listening to completion of the different registration steps.
		/// </summary>
		/// <param name="Sender">The event sender.</param>
		/// <param name="e">The default event args.</param>
		protected internal async void RegistrationStep_Completed(object Sender, EventArgs e)
		{
			try
			{
				switch (((RegistrationStepViewModel)Sender).Step)
				{
					case RegistrationStep.Account:
						// User connected to an existing account (as opposed to creating a new one). Copy values from the legal identity.
						if (this.TagProfile.LegalIdentity is not null)
						{
							RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
							vm.PopulateFromTagProfile();
						}
						break;
				}

				await this.SyncTagProfileStep();
			}
			catch (Exception Exception)
			{
				this.LogService?.LogException(Exception);
			}
		}

		private async void GoToPrev()
		{
			try
			{
				switch ((RegistrationStep)this.CurrentStep)
				{
					case RegistrationStep.Account:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearAccount();
						break;

					case RegistrationStep.RegisterIdentity:
						this.RegistrationSteps[(int)RegistrationStep.Account].ClearStepState();
						await this.TagProfile.ClearAccount(false);
						this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity].ClearStepState();
						await this.TagProfile.ClearLegalIdentity();
						await this.TagProfile.InvalidateContactInfo();
						break;

					case RegistrationStep.ValidateIdentity:
						RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
						vm.PopulateFromTagProfile();
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearIsValidated();
						break;

					case RegistrationStep.Pin:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.RevertPinStep();
						break;

					default: // RegistrationStep.Operator
						await this.TagProfile.ClearDomain();
						break;
				}

				await this.SyncTagProfileStep();
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task SyncTagProfileStep()
		{
			if (this.TagProfile.Step == RegistrationStep.Complete)
				await App.Current.SetAppShellPageAsync();
			else
				this.CurrentStep = (int)this.TagProfile.Step;
		}
	}
}
