using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using EDaler;
using EDaler.Uris;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Pages.Wallet;
using IdApp.Pages.Wallet.Payment;
using IdApp.Services;
using Waher.Persistence;
using Waher.Persistence.Filters;
using Xamarin.Forms;
using IdApp.Pages.Contacts.Chat;

namespace IdApp.Pages.Contacts
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ContactListViewModel : BaseViewModel
	{
		private TaskCompletionSource<ContactInfo> selection;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		protected internal ContactListViewModel()
		{
			this.Contacts = new ObservableCollection<ContactInfo>();

			this.Description = AppResources.ContactsDescription;
			this.Action = SelectContactAction.ViewIdentity;
			this.selection = null;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.NavigationService.TryPopArgs(out ContactListNavigationArgs args))
			{
				this.Description = args.Description;
				this.Action = args.Action;
				this.selection = args.Selection;
			}

			SortedDictionary<string, ContactInfo> Sorted = new SortedDictionary<string, ContactInfo>();
			string Name;
			string Suffix;
			int i;

			foreach (ContactInfo Info in await Database.Find<ContactInfo>(new FilterFieldNotEqualTo("IsThing", true)))	// Includes thos with IsThing=null
			{
				if (Info.AllowSubscriptionFrom.HasValue && !Info.AllowSubscriptionFrom.Value)
					continue;

				Name = Info.FriendlyName;
				if (Sorted.ContainsKey(Name))
				{
					i = 1;

					do
					{
						Suffix = " " + (++i).ToString();
					}
					while (Sorted.ContainsKey(Name + Suffix));

					Sorted[Name + Suffix] = Info;
				}
				else
					Sorted[Name] = Info;
			}

			this.Contacts.Clear();
			foreach (ContactInfo Info in Sorted.Values)
				this.Contacts.Add(Info);

			this.ShowContactsMissing = Sorted.Count == 0;
		}

		/// <inheritdoc/>
		protected override async Task DoUnbind()
		{
			if (this.Action != SelectContactAction.Select)
			{
				this.ShowContactsMissing = false;
				this.Contacts.Clear();
			}

			this.selection?.TrySetResult(null);

			await base.DoUnbind();
		}

		/// <summary>
		/// See <see cref="ShowContactsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContactsMissingProperty =
			BindableProperty.Create("ShowContactsMissing", typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowContactsMissing
		{
			get { return (bool)GetValue(ShowContactsMissingProperty); }
			set { SetValue(ShowContactsMissingProperty, value); }
		}

		/// <summary>
		/// <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create("Description", typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public string Description
		{
			get { return (string)GetValue(DescriptionProperty); }
			set { SetValue(DescriptionProperty, value); }
		}

		/// <summary>
		/// <see cref="Action"/>
		/// </summary>
		public static readonly BindableProperty ActionProperty =
			BindableProperty.Create("Action", typeof(SelectContactAction), typeof(ContactListViewModel), default(SelectContactAction));

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		public SelectContactAction Action
		{
			get { return (SelectContactAction)GetValue(ActionProperty); }
			set { SetValue(ActionProperty, value); }
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableCollection<ContactInfo> Contacts { get; }

		/// <summary>
		/// See <see cref="SelectedContact"/>
		/// </summary>
		public static readonly BindableProperty SelectedContactProperty =
			BindableProperty.Create("SelectedContact", typeof(ContactInfo), typeof(ContactListViewModel), default(ContactInfo),
				propertyChanged: (b, oldValue, newValue) =>
				{
					if (b is ContactListViewModel viewModel && newValue is ContactInfo Contact)
					{
						viewModel.UiSerializer.BeginInvokeOnMainThread(async () =>
						{
							switch (viewModel.Action)
							{
								case SelectContactAction.MakePayment:
									StringBuilder sb = new StringBuilder();

									sb.Append("edaler:");

									if (!string.IsNullOrEmpty(Contact.LegalId))
									{
										sb.Append("ti=");
										sb.Append(Contact.LegalId);
									}
									else if (!string.IsNullOrEmpty(Contact.BareJid))
									{
										sb.Append("t=");
										sb.Append(Contact.BareJid);
									}
									else
										break;

									Balance Balance = await viewModel.NeuronService.Wallet.GetBalanceAsync();

									sb.Append(";cu=");
									sb.Append(Balance.Currency);

									if (!EDalerUri.TryParse(sb.ToString(), out EDalerUri Parsed))
										break;

									await viewModel.NavigationService.GoToAsync(nameof(PaymentPage), new EDalerUriNavigationArgs(Parsed)
									{
										ReturnRoute = "../.."
									});
									break;

								case SelectContactAction.ViewIdentity:
								default:
									if (!(Contact.LegalIdentity is null))
									{
										await viewModel.NavigationService.GoToAsync(nameof(ViewIdentityPage),
											new ViewIdentityNavigationArgs(Contact.LegalIdentity, null));
									}
									else if (!string.IsNullOrEmpty(Contact.BareJid))
									{
										await viewModel.NavigationService.GoToAsync(nameof(ChatPage),
											new ChatNavigationArgs(Contact.LegalId, Contact.BareJid, Contact.FriendlyName));
									}
									break;

								case SelectContactAction.Select:
									viewModel.selection?.TrySetResult(Contact);
									await viewModel.NavigationService.GoBackAsync();
									break;
							}
						});
					}
				});

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfo SelectedContact
		{
			get { return (ContactInfo)GetValue(SelectedContactProperty); }
			set { SetValue(SelectedContactProperty, value); }
		}

	}
}