using IdApp.Extensions;
using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using IdApp.Pages.Petitions.PetitionContract;
using IdApp.Services.UI.Photos;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Notification.Contracts
{
	/// <summary>
	/// Notification event for contract petitions.
	/// </summary>
	public class ContractPetitionNotificationEvent : ContractNotificationEvent
	{
		private LegalIdentity identity;

		/// <summary>
		/// Notification event for contract petitions.
		/// </summary>
		public ContractPetitionNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for contract petitions.
		/// </summary>
		/// <param name="Contract">Requested contract.</param>
		/// <param name="e">Event arguments.</param>
		public ContractPetitionNotificationEvent(Contract Contract, ContractPetitionEventArgs e)
			: base(Contract, e)
		{
			this.Identity = e.RequestorIdentity;
			this.RequestorFullJid = e.RequestorFullJid;
			this.PetitionId = e.PetitionId;
			this.Purpose = e.Purpose;
		}

		/// <summary>
		/// Full JID of requestor.
		/// </summary>
		public string RequestorFullJid { get; }

		/// <summary>
		/// Petition ID
		/// </summary>
		public string PetitionId { get; set; }

		/// <summary>
		/// Purpose message, to be displayed to user.
		/// </summary>
		public string Purpose { get; set; }

		/// <summary>
		/// XML of identity.
		/// </summary>
		public string IdentityXml { get; set; }

		/// <summary>
		/// Gets a parsed identity.
		/// </summary>
		/// <returns>Parsed identity</returns>
		public LegalIdentity Identity
		{
			get
			{
				if (this.identity is null && !string.IsNullOrEmpty(this.IdentityXml))
				{
					XmlDocument Doc = new()
					{
						PreserveWhitespace = true
					};
					Doc.LoadXml(this.IdentityXml);

					this.identity = LegalIdentity.Parse(Doc.DocumentElement);
				}

				return this.identity;
			}

			set
			{
				this.identity = value;

				if (value is null)
					this.IdentityXml = null;
				else
				{
					StringBuilder Xml = new();
					value.Serialize(Xml, true, true, true, true, true, true, true);
					this.IdentityXml = Xml.ToString();
				}
			}
		}

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(IServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();

			if (Contract is not null)
			{
				await ServiceReferences.NavigationService.GoToAsync(nameof(PetitionContractPage),
					new PetitionContractNavigationArgs(this.Identity, this.RequestorFullJid, Contract, this.PetitionId, this.Purpose));
			}
		}

		/// <summary>
		/// Gets a descriptive text for the category of event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task<string> GetDescription(IServiceReferences ServiceReferences)
		{
			Contract Contract = await this.GetContract();
			StringBuilder Result = new();

			Result.Append(LocalizationResourceManager.Current["RequestToAccessContract"]);

			if (Contract is not null)
			{
				Result.Append(": ");
				Result.Append(await ContractModel.GetCategory(Contract));
			}

			Result.Append('.');

			return Result.ToString();
		}

		/// <summary>
		/// Performs perparatory tasks, that will simplify opening the notification.
		/// </summary>
		public override async Task Prepare(IServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (Identity?.Attachments is not null)
			{
				foreach (Attachment Attachment in Identity.Attachments.GetImageAttachments())
				{
					try
					{
						await PhotosLoader.LoadPhoto(Attachment);
					}
					catch (Exception ex)
					{
						ServiceReferences.LogService.LogException(ex);
					}
				}
			}
		}

	}
}
