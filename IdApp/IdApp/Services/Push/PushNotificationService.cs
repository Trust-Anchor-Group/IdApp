using EDaler;
using IdApp.DeviceSpecific;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Provisioning;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly Dictionary<PushMessagingService, string> tokens = new();
		private DateTime lastTokenCheck = DateTime.MinValue;

		/// <summary>
		/// Push notification service
		/// </summary>
		public PushNotificationService()
		{
		}

		/// <summary>
		/// New token received from push notification back-end.
		/// </summary>
		/// <param name="TokenInformation">Token information</param>
		public async Task NewToken(TokenInformation TokenInformation)
		{
			lock (this.tokens)
			{
				this.tokens[TokenInformation.Service] = TokenInformation.Token;
			}

			await this.XmppService.NewPushNotificationToken(TokenInformation);

			try
			{
				this.OnNewToken?.Invoke(this, new TokenEventArgs(TokenInformation.Service, TokenInformation.Token, TokenInformation.ClientType));
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Event raised when a new token is made available.
		/// </summary>
		public event TokenEventHandler OnNewToken;

		/// <summary>
		/// Tries to get a token from a push notification service.
		/// </summary>
		/// <param name="Source">Source of token</param>
		/// <param name="Token">Token, if found.</param>
		/// <returns>If a token was found for the corresponding source.</returns>
		public bool TryGetToken(PushMessagingService Source, out string Token)
		{
			lock (this.tokens)
			{
				return this.tokens.TryGetValue(Source, out Token);
			}
		}

		private async Task<bool> ForceTokenReport(TokenInformation TokenInformation)
		{
			string OldToken = await RuntimeSettings.GetAsync("PUSH.TOKEN", string.Empty);
			DateTime ReportDate = await RuntimeSettings.GetAsync("PUSH.REPORT_DATE", DateTime.MinValue);

			return (DateTime.UtcNow.Subtract(ReportDate).TotalDays > 7) || (TokenInformation.Token != OldToken);
		}

		/// <summary>
		/// Checks if the Push Notification Token is current and registered properly.
		/// </summary>
		/// <param name="TokenInformation">Non null if we got it from the OnNewToken</param>
		public async Task CheckPushNotificationToken(TokenInformation TokenInformation)
		{
			try
			{
				Log.Warning("CheckPushNotificationToken 1");
				DateTime Now = DateTime.Now;

				if (this.XmppService.IsOnline &&
					this.XmppService.SupportsPushNotification &&
					Now.Subtract(this.lastTokenCheck).TotalHours >= 1)
				{
					Log.Warning("CheckPushNotificationToken 2");
					this.lastTokenCheck = Now;

					IGetPushNotificationToken GetToken = DependencyService.Get<IGetPushNotificationToken>();
					if (GetToken is null)
						return;

					if (TokenInformation is null)
					{
						Log.Warning("CheckPushNotificationToken 3");
						TokenInformation = await GetToken.GetToken();
						if (string.IsNullOrEmpty(TokenInformation.Token))
							return;
					}

					bool ForceTokenReport = await this.ForceTokenReport(TokenInformation);

					string Version = AppInfo.VersionString + "." + AppInfo.BuildString;
					string PrevVersion = await RuntimeSettings.GetAsync("PUSH.CONFIG_VERSION", string.Empty);
					bool IsVersionChanged = Version != PrevVersion;

					if (IsVersionChanged || ForceTokenReport)
					{
						Log.Warning("CheckPushNotificationToken 4", PrevVersion);
						string Token = TokenInformation.Token;
						PushMessagingService Service = TokenInformation.Service;
						ClientType ClientType = TokenInformation.ClientType;
						await this.XmppService.ReportNewPushNotificationToken(Token, Service, ClientType);

						await RuntimeSettings.SetAsync("PUSH.TOKEN", TokenInformation.Token);
						await RuntimeSettings.SetAsync("PUSH.REPORT_DATE", DateTime.UtcNow);
						Log.Warning("CheckPushNotificationToken 5", Version);
					}

					if (IsVersionChanged)
					{
						Log.Warning("CheckPushNotificationToken 6");
						// it will force the rules update if somehing goes wrong.
						await RuntimeSettings.SetAsync("PUSH.CONFIG_VERSION", string.Empty);
						await this.XmppService.ClearPushNotificationRules();

						#region Message Rules

						// Push Notification Rule, for chat messages received when offline:

						StringBuilder Content = new();

						Content.Append("FromJid:=GetAttribute(Stanza,'from');");
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("FriendlyName:=RosterName(ToJid,FromJid);");
						Content.Append("Content:=GetElement(Stanza,'content');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["MessageFrom"]));
						Content.Append(" ' + FriendlyName,");
						Content.Append("'myBody':InnerText(GetElement(Stanza,'body')),");
						Content.Append("'fromJid':FromJid,");
						Content.Append("'rosterName':FriendlyName,");
						//Content.Append("'isObject':false,");
						Content.Append("'isObject':exists(Content) and !empty(Markdown:= InnerText(Content)) and (Left(Markdown,2)='![' or (Left(Markdown,3)='```' and Right(Markdown,3)='```')),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Messages);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Chat, string.Empty, string.Empty,
							Constants.PushChannels.Messages, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region Petitions

						// Push Notification Rule, for Identity Petition requests when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'petitionIdentityMsg');");
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("FromJid:=GetAttribute(E,'from');");
						Content.Append("FriendlyName:=RosterName(ToJid,FromJid);");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["PetitionFrom"]));
						Content.Append(" ' + FriendlyName,");
						Content.Append("'myBody':GetAttribute(E,'purpose'),");
						Content.Append("'fromJid':FromJid,");
						Content.Append("'rosterName':FriendlyName,");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Petitions);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "petitionIdentityMsg", ContractsClient.NamespaceLegalIdentities,
							Constants.PushChannels.Petitions, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Contract Petition requests when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'petitionContractMsg');");
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("FromJid:=GetAttribute(E,'from');");
						Content.Append("FriendlyName:=RosterName(ToJid,FromJid);");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["PetitionFrom"]));
						Content.Append(" ' + FriendlyName,");
						Content.Append("'myBody':GetAttribute(E,'purpose'),");
						Content.Append("'fromJid':FromJid,");
						Content.Append("'rosterName':FriendlyName,");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Petitions);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "petitionContractMsg", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Petitions, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Signature Petition requests when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'petitionSignatureMsg');");
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("FromJid:=GetAttribute(E,'from');");
						Content.Append("FriendlyName:=RosterName(ToJid,FromJid);");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["PetitionFrom"]));
						Content.Append(" ' + FriendlyName,");
						Content.Append("'myBody':GetAttribute(E,'purpose'),");
						Content.Append("'fromJid':FromJid,");
						Content.Append("'rosterName':FriendlyName,");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Petitions);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "petitionSignatureMsg", ContractsClient.NamespaceLegalIdentities,
							Constants.PushChannels.Petitions, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region Identities

						// Push Notification Rule, for Identity Update events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'identity');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["IdentityUpdated"]));
						Content.Append("',");
						Content.Append("'legalId':GetAttribute(E,'id'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Identities);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "identity", ContractsClient.NamespaceLegalIdentities,
							Constants.PushChannels.Identities, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region Contracts

						// Push Notification Rule, for Contract Creation events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'contractCreated');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ContractCreated"]));
						Content.Append("',");
						Content.Append("'contractId':GetAttribute(E,'contractId'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Contracts);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "contractCreated", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Contracts, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Contract Signature events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'contractSigned');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ContractSigned"]));
						Content.Append("',");
						Content.Append("'contractId':GetAttribute(E,'contractId'),");
						Content.Append("'legalId':GetAttribute(E,'legalId'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Contracts);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "contractSigned", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Contracts, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Contract Update events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'contractUpdated');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ContractUpdated"]));
						Content.Append("',");
						Content.Append("'contractId':GetAttribute(E,'contractId'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Contracts);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "contractUpdated", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Contracts, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Contract Deletion events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'contractDeleted');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ContractDeleted"]));
						Content.Append("',");
						Content.Append("'contractId':GetAttribute(E,'contractId'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Contracts);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "contractDeleted", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Contracts, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for Contract Proposal events when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'contractProposal');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ContractProposed"]));
						Content.Append("',");
						Content.Append("'myBody':GetAttribute(E,'message'),");
						Content.Append("'contractId':Num(GetAttribute(E,'contractId')),");
						Content.Append("'role':Num(GetAttribute(E,'role')),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Contracts);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "contractProposal", ContractsClient.NamespaceSmartContracts,
							Constants.PushChannels.Contracts, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region eDaler

						// Push Notification Rule, for eDaler balance updates when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'balance');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["BalanceUpdated"]));
						Content.Append("',");
						Content.Append("'amount':Num(GetAttribute(E,'amount')),");
						Content.Append("'currency':GetAttribute(E,'currency'),");
						Content.Append("'timestamp':DateTime(GetAttribute(E,'timestamp')),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.EDaler);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "balance", EDalerClient.NamespaceEDaler,
							Constants.PushChannels.EDaler, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region Neuro-Features

						// Push Notification Rule, for token additions when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'tokenAdded');");
						Content.Append("E2:=GetElement(E,'token');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["TokenAdded"]));
						Content.Append("',");
						Content.Append("'myBody':GetAttribute(E2,'friendlyName'),");
						Content.Append("'value':Num(GetAttribute(E2,'value')),");
						Content.Append("'currency':GetAttribute(E2,'currency'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Tokens);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "tokenAdded", NeuroFeaturesClient.NamespaceNeuroFeatures,
							Constants.PushChannels.Tokens, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for token removals when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'tokenRemoved');");
						Content.Append("E2:=GetElement(E,'token');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["TokenRemoved"]));
						Content.Append("',");
						Content.Append("'myBody':GetAttribute(E2,'friendlyName'),");
						Content.Append("'value':Num(GetAttribute(E2,'value')),");
						Content.Append("'currency':GetAttribute(E2,'currency'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Tokens);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "tokenRemoved", NeuroFeaturesClient.NamespaceNeuroFeatures,
							Constants.PushChannels.Tokens, "Stanza", string.Empty, Content.ToString());

						#endregion

						#region Provisioning

						// Push Notification Rule, for friendship requests from things when offline.

						Content.Clear();
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("E:=GetElement(Stanza,'isFriend');");
						Content.Append("RemoteJid:=GetAttribute(E,'remoteJid');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["AccessRequest"]));
						Content.Append("',");
						Content.Append("'myBody':RosterName(ToJid,RemoteJid),");
						Content.Append("'remoteJid':RemoteJid,");
						Content.Append("'jid':GetAttribute(E,'jid'),");
						Content.Append("'key':GetAttribute(E,'key'),");
						Content.Append("'q':'isFriend',");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Provisioning);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "isFriend", ProvisioningClient.NamespaceProvisioningOwner,
							Constants.PushChannels.Provisioning, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for readout requests from things when offline.

						Content.Clear();
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("E:=GetElement(Stanza,'canRead');");
						Content.Append("RemoteJid:=GetAttribute(E,'remoteJid');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ReadRequest"]));
						Content.Append("',");
						Content.Append("'myBody':RosterName(ToJid,RemoteJid),");
						Content.Append("'remoteJid':RemoteJid,");
						Content.Append("'jid':GetAttribute(E,'jid'),");
						Content.Append("'key':GetAttribute(E,'key'),");
						Content.Append("'q':'canRead',");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Provisioning);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "canRead", ProvisioningClient.NamespaceProvisioningOwner,
							Constants.PushChannels.Provisioning, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for control requests from things when offline.

						Content.Clear();
						Content.Append("ToJid:=GetAttribute(Stanza,'to');");
						Content.Append("E:=GetElement(Stanza,'canControl');");
						Content.Append("RemoteJid:=GetAttribute(E,'remoteJid');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["ControlRequest"]));
						Content.Append("',");
						Content.Append("'myBody':RosterName(ToJid,RemoteJid),");
						Content.Append("'remoteJid':RemoteJid,");
						Content.Append("'jid':GetAttribute(E,'jid'),");
						Content.Append("'key':GetAttribute(E,'key'),");
						Content.Append("'q':'canControl',");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Provisioning);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await this.XmppService.AddPushNotificationRule(MessageType.Normal, "canControl", ProvisioningClient.NamespaceProvisioningOwner,
							Constants.PushChannels.Provisioning, "Stanza", string.Empty, Content.ToString());

						#endregion

						await RuntimeSettings.SetAsync("PUSH.CONFIG_VERSION", Version);
						Log.Warning("CheckPushNotificationToken 7", Version);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}
	}
}
