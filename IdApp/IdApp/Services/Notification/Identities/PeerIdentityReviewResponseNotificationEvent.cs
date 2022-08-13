using IdApp.Resx;
using System;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Services.Notification.Identities
{
	/// <summary>
	/// Notification event for peer review responses of identities.
	/// </summary>
	public class PeerIdentityReviewResponseNotificationEvent : IdentityNotificationEvent
	{
		/// <summary>
		/// Notification event for peer review responses of identities.
		/// </summary>
		public PeerIdentityReviewResponseNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Notification event for peer review responses of identities.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public PeerIdentityReviewResponseNotificationEvent(SignaturePetitionResponseEventArgs e)
			: base(e)
		{
			this.Response = e.Response;
		}

		/// <summary>
		/// Response
		/// </summary>
		public bool Response { get; set; }

		/// <summary>
		/// Opens the event.
		/// </summary>
		/// <param name="ServiceReferences">Service references</param>
		public override async Task Open(ServiceReferences ServiceReferences)
		{
			LegalIdentity Identity = this.Identity;

			if (Identity is not null)
			{
				try
				{
					if (!this.Response)
						await ServiceReferences.UiSerializer.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasRejected, AppResources.Ok);
					else
					{
						StringBuilder Xml = new();
						ServiceReferences.TagProfile.LegalIdentity.Serialize(Xml, true, true, true, true, true, true, true);
						byte[] Data = Encoding.UTF8.GetBytes(Xml.ToString());
						bool? Result;

						try
						{
							Result = ServiceReferences.XmppService.Contracts.ValidateSignature(this.Identity, Data, this.ContentToSign);
							// ContentToSign contains signature
						}
						catch (Exception ex)
						{
							await ServiceReferences.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message);
							return;
						}

						if (!Result.HasValue || !Result.Value)
							await ServiceReferences.UiSerializer.DisplayAlert(AppResources.PeerReviewRejected, AppResources.APeerYouRequestedToReviewHasBeenRejectedDueToSignatureError, AppResources.Ok);
						else
						{
							(bool Succeeded, LegalIdentity LegalIdentity) = await ServiceReferences.NetworkService.TryRequest(
								() => ServiceReferences.XmppService.Contracts.AddPeerReviewIdAttachment(
									ServiceReferences.TagProfile.LegalIdentity, this.Identity, this.ContentToSign));
							// ContentToSign contains signature

							if (Succeeded)
								await ServiceReferences.UiSerializer.DisplayAlert(AppResources.PeerReviewAccepted, AppResources.APeerReviewYouhaveRequestedHasBeenAccepted, AppResources.Ok);
						}
					}
				}
				catch (Exception ex)
				{
					ServiceReferences.LogService.LogException(ex);
					await ServiceReferences.UiSerializer.DisplayAlert(AppResources.ErrorTitle, ex.Message, AppResources.Ok);
				}
			}
		}
	}
}
