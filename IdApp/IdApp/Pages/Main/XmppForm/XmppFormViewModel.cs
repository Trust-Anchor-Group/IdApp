﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Waher.Networking.XMPP.DataForms;
using Layout = Waher.Networking.XMPP.DataForms.Layout;
using Xamarin.Forms;
using Waher.Networking.XMPP.DataForms.FieldTypes;
using System.Collections.Generic;
using IdApp.Pages.Main.XmppForm.Model;

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
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out XmppFormNavigationArgs args))
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
							this.Pages.Add(new PageModel(P));
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

						this.Pages.Add(new PageModel(new Layout.Page(this.form, string.Empty, Elements.ToArray())));
					}

					args.ViewInitialized = true;
				}
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

		#endregion

		#region Commands

		#endregion
	}
}
