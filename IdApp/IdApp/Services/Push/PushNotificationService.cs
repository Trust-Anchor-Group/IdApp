using EDaler;
using IdApp.DeviceSpecific;
using IdApp.Services.Xmpp;
using NeuroFeatures;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Xamarin.CommunityToolkit.Helpers;
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

		/// <summary>
		/// Checks if the Push Notification Token is current and registered properly.
		/// </summary>
		public async Task CheckPushNotificationToken()
		{
			try
			{
				DateTime Now = DateTime.Now;
				PushNotificationClient PushNotificationClient = (this.XmppService as XmppService)?.PushNotificationClient;

				if (this.XmppService.IsOnline && (PushNotificationClient is not null) && Now.Subtract(this.lastTokenCheck).TotalHours >= 1)
				{
					this.lastTokenCheck = Now;

					DateTime TP = await RuntimeSettings.GetAsync("PUSH.TP", DateTime.MinValue);
					DateTime LastTP = await RuntimeSettings.GetAsync("PUSH.LAST_TP", DateTime.MinValue);
					bool Reconfig = false;

					if (TP != LastTP || DateTime.UtcNow.Subtract(LastTP).TotalDays >= 7)    // Firebase recommends updating token, while app still works, but not more often than once a week, unless it changes.
					{
						string Token = await RuntimeSettings.GetAsync("PUSH.TOKEN", string.Empty);
						PushMessagingService Service;
						ClientType ClientType;

						if (string.IsNullOrEmpty(Token))
						{
							IGetPushNotificationToken GetToken = DependencyService.Get<IGetPushNotificationToken>();
							if (GetToken is null)
								return;

							TokenInformation TokenInformation = await GetToken.GetToken();

							Token = TokenInformation.Token;
							if (string.IsNullOrEmpty(Token))
								return;

							Service = TokenInformation.Service;
							ClientType = TokenInformation.ClientType;

							await RuntimeSettings.SetAsync("PUSH.TOKEN", Token);
							await RuntimeSettings.SetAsync("PUSH.SERVICE", Service);
							await RuntimeSettings.SetAsync("PUSH.CLIENT", ClientType);
						}
						else
						{
							Service = (PushMessagingService)await RuntimeSettings.GetAsync("PUSH.SERVICE", PushMessagingService.Firebase);
							ClientType = (ClientType)await RuntimeSettings.GetAsync("PUSH.CLIENT", ClientType.Other);
						}

						await PushNotificationClient.NewTokenAsync(Token, Service, ClientType);
						await RuntimeSettings.SetAsync("PUSH.LAST_TP", TP);

						Reconfig = true;
					}

					long ConfigNr = await RuntimeSettings.GetAsync("PUSH.CONFIG_NR", 0);
					if (ConfigNr != currentTokenConfiguration || Reconfig)
					{
						await RuntimeSettings.SetAsync("PUSH.CONFIG_NR", 0);
						await PushNotificationClient.ClearRulesAsync();

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

						await PushNotificationClient.AddRuleAsync(MessageType.Chat, string.Empty, string.Empty,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "petitionIdentityMsg", ContractsClient.NamespaceLegalIdentities,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "petitionContractMsg", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "petitionSignatureMsg", ContractsClient.NamespaceLegalIdentities,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "identity", ContractsClient.NamespaceLegalIdentities,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "contractCreated", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "contractSigned", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "contractUpdated", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "contractDeleted", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "contractProposal", ContractsClient.NamespaceSmartContracts,
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

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "balance", EDalerClient.NamespaceEDaler,
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
						Content.Append("'myBody':DateTime(GetAttribute(E,'friendlyName')),");
						Content.Append("'value':Num(GetAttribute(E,'value')),");
						Content.Append("'currency':GetAttribute(E2,'currency'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Tokens);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "tokenAdded", NeuroFeaturesClient.NamespaceNeuroFeatures,
							Constants.PushChannels.Tokens, "Stanza", string.Empty, Content.ToString());

						// Push Notification Rule, for token removals when offline.

						Content.Clear();
						Content.Append("E:=GetElement(Stanza,'tokenRemoved');");
						Content.Append("E2:=GetElement(E,'token');");
						Content.Append("{'myTitle':'");
						Content.Append(JSON.Encode(LocalizationResourceManager.Current["TokenRemoved"]));
						Content.Append("',");
						Content.Append("'myBody':DateTime(GetAttribute(E,'friendlyName')),");
						Content.Append("'value':Num(GetAttribute(E,'value')),");
						Content.Append("'currency':GetAttribute(E2,'currency'),");
						Content.Append("'channelId':'");
						Content.Append(Constants.PushChannels.Tokens);
						Content.Append("',");
						Content.Append("'content_available':true}");

						await PushNotificationClient.AddRuleAsync(MessageType.Normal, "tokenRemoved", NeuroFeaturesClient.NamespaceNeuroFeatures,
							Constants.PushChannels.Tokens, "Stanza", string.Empty, Content.ToString());

						#endregion

						await RuntimeSettings.SetAsync("PUSH.CONFIG_NR", currentTokenConfiguration);
					}
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}
		}

		/// <summary>
		/// Increment this configuration number by one, each time token configuration changes.
		/// </summary>
		private const int currentTokenConfiguration = 10;

	}
}
