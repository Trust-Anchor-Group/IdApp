using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Waher.Networking.XMPP.DataForms;
using Layout = Waher.Networking.XMPP.DataForms.Layout;
using Xamarin.Forms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using System.Collections.Generic;
using IdApp.Pages.Main.XmppForm.Model;
using System.Windows.Input;
using System;
using Waher.Script.Functions.ComplexNumbers;

namespace IdApp.Pages.Main.XmppForm
{
	/// <summary>
	/// The view model to bind to for when displaying the calculator.
	/// </summary>
	public class XmppFormViewModel : XmppViewModel
	{
		private DataForm form;
		private bool responseSent;

		/// <summary>
		/// Creates an instance of the <see cref="XmppFormViewModel"/> class.
		/// </summary>
		public XmppFormViewModel()
			: base()
		{
			this.Pages = new ObservableCollection<PageModel>();

			this.SubmitCommand = new Command(async (_) => await this.ExecuteSubmit());
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			//!!!!!!
			/*
			if (this.NavigationService.TryGetArgs(out XmppFormNavigationArgs args))
			{
				if (!args.ViewInitialized)
				{
					this.form = args.Form;
					this.responseSent = false;

					// TODO: Post-back fields.

					this.Title = this.form.Title;
					this.Instructions = this.form.Instructions;

					if (this.form.HasPages)
					{
						foreach (Layout.Page P in this.form.Pages)
							this.Pages.Add(new PageModel(this, P));

						this.MultiplePages = this.form.Pages.Length > 1;
					}
					else
					{
						List<Layout.LayoutElement> Elements = new();

						foreach (Field F in this.form.Fields)
						{
							if (F is HiddenField)
								continue;

							Elements.Add(new Layout.FieldReference(this.form, F.Var));
						}

						this.Pages.Add(new PageModel(this, new Layout.Page(this.form, string.Empty, Elements.ToArray())));

						this.MultiplePages = false;
					}

					this.ValidateForm();

					args.ViewInitialized = true;
				}
			}
			*/

			if (this.NavigationService.TryGetArgs(out XmppFormNavigationArgs Args))
			{
				this.form = Args.Form;
				this.responseSent = false;

				// TODO: Post-back fields.

				this.Title = this.form.Title;
				this.Instructions = this.form.Instructions;

				if (this.form.HasPages)
				{
					foreach (Layout.Page P in this.form.Pages)
						this.Pages.Add(new PageModel(this, P));

					this.MultiplePages = this.form.Pages.Length > 1;
				}
				else
				{
					List<Layout.LayoutElement> Elements = new();

					foreach (Field F in this.form.Fields)
					{
						if (F is HiddenField)
							continue;

						Elements.Add(new Layout.FieldReference(this.form, F.Var));
					}

					this.Pages.Add(new PageModel(this, new Layout.Page(this.form, string.Empty, Elements.ToArray())));

					this.MultiplePages = false;
				}

				this.ValidateForm();
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDispose()
		{
			if (this.form is not null && this.form.CanCancel && !this.responseSent)
			{
				this.form.Cancel();
				this.responseSent = true;
			}

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// See <see cref="Title"/>
		/// </summary>
		public static readonly BindableProperty TitleProperty =
			BindableProperty.Create(nameof(Title), typeof(string), typeof(XmppFormViewModel), default(string));

		/// <summary>
		/// Title of form
		/// </summary>
		public string Title
		{
			get => (string)this.GetValue(TitleProperty);
			set => this.SetValue(TitleProperty, value);
		}

		/// <summary>
		/// See <see cref="Instructions"/>
		/// </summary>
		public static readonly BindableProperty InstructionsProperty =
			BindableProperty.Create(nameof(Instructions), typeof(string[]), typeof(XmppFormViewModel), default(string[]));

		/// <summary>
		/// Instructions for form
		/// </summary>
		public string[] Instructions
		{
			get => (string[])this.GetValue(InstructionsProperty);
			set => this.SetValue(InstructionsProperty, value);
		}

		/// <summary>
		/// Holds the pages of the form
		/// </summary>
		public ObservableCollection<PageModel> Pages { get; }

		/// <summary>
		/// See <see cref="IsFormOk"/>
		/// </summary>
		public static readonly BindableProperty IsFormOkProperty =
			BindableProperty.Create(nameof(IsFormOk), typeof(bool), typeof(XmppFormViewModel), default(bool));

		/// <summary>
		/// IsFormOk of form
		/// </summary>
		public bool IsFormOk
		{
			get => (bool)this.GetValue(IsFormOkProperty);
			set => this.SetValue(IsFormOkProperty, value);
		}

		/// <summary>
		/// See <see cref="MultiplePages"/>
		/// </summary>
		public static readonly BindableProperty MultiplePagesProperty =
			BindableProperty.Create(nameof(MultiplePages), typeof(bool), typeof(XmppFormViewModel), default(bool));

		/// <summary>
		/// MultiplePages of form
		/// </summary>
		public bool MultiplePages
		{
			get => (bool)this.GetValue(MultiplePagesProperty);
			set => this.SetValue(MultiplePagesProperty, value);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Command executed when user wants to submit the form.
		/// </summary>
		public ICommand SubmitCommand { get; }

		private async Task ExecuteSubmit()
		{
			if (!this.IsFormOk)
				return;

			try
			{
				if (this.form is not null && this.form.CanSubmit && !this.responseSent)
				{
					this.form.Submit();
					this.responseSent = true;

					await this.NavigationService.GoBackAsync();
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		internal void ValidateForm()
		{
			try
			{
				foreach (Field F in this.form.Fields)
				{
					if (F.HasError)
					{
						this.IsFormOk = false;
						return;
					}
				}

				this.IsFormOk = true;
			}
			catch (Exception)
			{
				this.IsFormOk = false;
			}
		}

		#endregion
	}
}
