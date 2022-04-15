using IdApp.DeviceSpecific;
using IdApp.Resx;
using IdApp.Services.Xmpp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Events;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Push;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Xamarin.Forms;

namespace IdApp.Services.Push
{
	/// <summary>
	/// Push notification service
	/// </summary>
	[Singleton]
	public class PushNotificationService : LoadableService, IPushNotificationService
	{
		private readonly Dictionary<Waher.Networking.XMPP.Push.PushMessagingService, string> tokens = new();
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
				Log.Critical(ex);
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
		public bool TryGetToken(Waher.Networking.XMPP.Push.PushMessagingService Source, out string Token)
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
			DateTime Now = DateTime.Now;
			PushNotificationClient PushNotificationClient = (this.XmppService as XmppService)?.PushNotificationClient;

			if (this.XmppService.IsOnline && !(PushNotificationClient is null) && Now.Subtract(this.lastTokenCheck).TotalHours >= 1)
			{
				this.lastTokenCheck = Now;

				DateTime TP = await RuntimeSettings.GetAsync("PUSH.TP", DateTime.MinValue);
				DateTime LastTP = await RuntimeSettings.GetAsync("PUSH.LAST_TP", DateTime.MinValue);
				bool Reconfig = false;

				if (TP != LastTP || DateTime.UtcNow.Subtract(LastTP).TotalDays >= 7)    // Firebase recommends updating token, while app still works, but not more often than once a week, unless it changes.
				{
					string Token = await RuntimeSettings.GetAsync("PUSH.TOKEN", string.Empty);
					Waher.Networking.XMPP.Push.PushMessagingService Service;
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
						Service = (Waher.Networking.XMPP.Push.PushMessagingService)await RuntimeSettings.GetAsync("PUSH.SERVICE", Waher.Networking.XMPP.Push.PushMessagingService.Firebase);
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

					// Push Notification rule for chat messages received when offline:

					StringBuilder Content = new StringBuilder();

					Content.Append("{'title':'");
					Content.Append(AppResources.ChatMessageReceived);
					Content.Append("','body':(select /default:message/default:body from Stanza),");
					Content.Append("'fromJid':(select /default:message/@from from Stanza)}");

					await PushNotificationClient.AddRuleAsync(MessageType.Chat, string.Empty, string.Empty, "Messages",
						"Stanza", string.Empty, Content.ToString());

					await RuntimeSettings.SetAsync("PUSH.CONFIG_NR", currentTokenConfiguration);
				}
			}
		}

		/// <summary>
		/// Increment this configuration number by one, each time token configuration changes.
		/// </summary>
		private const int currentTokenConfiguration = 2;

	}
}
