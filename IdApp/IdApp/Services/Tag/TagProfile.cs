using System;
using System.Text;
using IdApp.Extensions;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;
using Waher.Security;

namespace IdApp.Services.Tag
{
	/// <summary>
	/// The different steps of a TAG Profile registration journey.
	/// </summary>
	public enum RegistrationStep
	{
		/// <summary>
		/// Validate Phone Number
		/// </summary>
		ValidatePhoneNr = 0,

		/// <summary>
		/// Create or connect to an account
		/// </summary>
		Account = 1,

		/// <summary>
		/// Register an identity
		/// </summary>
		RegisterIdentity = 2,

		/// <summary>
		/// Have the identity validated.
		/// </summary>
		ValidateIdentity = 3,

		/// <summary>
		/// Create a PIN code
		/// </summary>
		Pin = 4,

		/// <summary>
		/// Profile is completed.
		/// </summary>
		Complete = 5
	}

	/// <inheritdoc/>
	[Singleton]
	public class TagProfile : ITagProfile
	{
		/// <summary>
		/// An event that fires every time the <see cref="Step"/> property changes.
		/// </summary>
		public event EventHandler StepChanged;
		/// <summary>
		/// An event that fires every time any property changes.
		/// </summary>
		public event System.ComponentModel.PropertyChangedEventHandler Changed;

		private LegalIdentity legalIdentity;
		private string objectId;
		private string domain;
		private string apiKey;
		private string apiSecret;
		private string phoneNumber;
		private string account;
		private string passwordHash;
		private string passwordHashMethod;
		private string legalJid;
		private string registryJid;
		private string provisioningJid;
		private string httpFileUploadJid;
		private string logJid;
		private string mucJid;
		private string eDalerJid;
		private string pinHash;
		private long? httpFileUploadMaxSize;
		private bool usePin;
		private RegistrationStep step = RegistrationStep.ValidatePhoneNr;
		private bool suppressPropertyChangedEvents;
		private bool defaultXmppConnectivity;

		/// <summary>
		/// Creates an instance of a <see cref="TagProfile"/>.
		/// </summary>
		public TagProfile()
		{
		}

		/// <summary>
		/// Invoked whenever the current <see cref="Step"/> changes, to fire the <see cref="StepChanged"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnStepChanged(EventArgs e)
		{
			StepChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Invoked whenever any property changes, to fire the <see cref="Changed"/> event.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnChanged(System.ComponentModel.PropertyChangedEventArgs e)
		{
			Changed?.Invoke(this, e);
		}

		/// <summary>
		/// Converts the current instance into a <see cref="TagConfiguration"/> object for serialization.
		/// </summary>
		/// <returns>Configuration object</returns>
		public TagConfiguration ToConfiguration()
		{
			TagConfiguration clone = new TagConfiguration
			{
				ObjectId = this.objectId,
				Domain = this.Domain,
				ApiKey = this.ApiKey,
				ApiSecret = this.ApiSecret,
				PhoneNumber = this.PhoneNumber,
				DefaultXmppConnectivity = this.DefaultXmppConnectivity,
				Account = this.Account,
				PasswordHash = this.PasswordHash,
				PasswordHashMethod = this.PasswordHashMethod,
				LegalJid = this.LegalJid,
				RegistryJid = this.RegistryJid,
				ProvisioningJid = this.ProvisioningJid,
				HttpFileUploadJid = this.HttpFileUploadJid,
				HttpFileUploadMaxSize = this.HttpFileUploadMaxSize,
				LogJid = this.LogJid,
				MucJid = this.MucJid,
				EDalerJid = this.EDalerJid,
				PinHash = this.PinHash,
				UsePin = this.UsePin,
				LegalIdentity = this.LegalIdentity,
				Step = this.Step
			};

			return clone;
		}

		/// <summary>
		/// Parses an instance of a <see cref="TagConfiguration"/> object to update this instance's properties.
		/// </summary>
		/// <param name="configuration"></param>
		public void FromConfiguration(TagConfiguration configuration)
		{
			try
			{
				this.suppressPropertyChangedEvents = true;

				this.objectId = configuration.ObjectId;
				this.Domain = configuration.Domain;
				this.ApiKey = configuration.ApiKey;
				this.ApiSecret = configuration.ApiSecret;
				this.PhoneNumber = configuration.PhoneNumber;
				this.DefaultXmppConnectivity = configuration.DefaultXmppConnectivity;
				this.Account = configuration.Account;
				this.PasswordHash = configuration.PasswordHash;
				this.PasswordHashMethod = configuration.PasswordHashMethod;
				this.LegalJid = configuration.LegalJid;
				this.RegistryJid = configuration.RegistryJid;
				this.ProvisioningJid = configuration.ProvisioningJid;
				this.HttpFileUploadJid = configuration.HttpFileUploadJid;
				this.HttpFileUploadMaxSize = configuration.HttpFileUploadMaxSize;
				this.LogJid = configuration.LogJid;
				this.MucJid = configuration.MucJid;
				this.EDalerJid = configuration.EDalerJid;
				this.PinHash = configuration.PinHash;
				this.UsePin = configuration.UsePin;
				this.LegalIdentity = configuration.LegalIdentity;

				// Do this last, as listeners will read the other properties when the event is fired.
				this.Step = configuration.Step;
			}
			finally
			{
				this.suppressPropertyChangedEvents = false;
			}
		}

		/// <inheritdoc/>
		public virtual bool NeedsUpdating()
		{
			return string.IsNullOrWhiteSpace(this.LegalJid) ||
				   string.IsNullOrWhiteSpace(this.RegistryJid) ||
				   string.IsNullOrWhiteSpace(this.ProvisioningJid) ||
				   string.IsNullOrWhiteSpace(this.HttpFileUploadJid) ||
				   string.IsNullOrWhiteSpace(this.LogJid) ||
				   string.IsNullOrWhiteSpace(this.MucJid) ||
				   string.IsNullOrWhiteSpace(this.EDalerJid);
		}

		/// <inheritdoc/>
		public virtual bool LegalIdentityNeedsUpdating()
		{
			return this.legalIdentity.NeedsUpdating();
		}

		/// <inheritdoc/>
		public virtual bool IsCompleteOrWaitingForValidation()
		{
			return this.Step >= RegistrationStep.ValidateIdentity;
		}

		/// <inheritdoc/>
		public virtual bool IsComplete()
		{
			return this.Step == RegistrationStep.Complete;
		}

		#region Properties

		/// <inheritdoc/>
		public bool DefaultXmppConnectivity
		{
			get => this.defaultXmppConnectivity;
			private set
			{
				if (this.defaultXmppConnectivity != value)
				{
					this.defaultXmppConnectivity = value;
					this.FlagAsDirty(nameof(DefaultXmppConnectivity));
				}
			}
		}

		/// <inheritdoc/>
		public string Domain
		{
			get => this.domain;
			private set
			{
				if (!string.Equals(this.domain, value))
				{
					this.domain = value;
					this.FlagAsDirty(nameof(Domain));
				}
			}
		}

		/// <inheritdoc/>
		public string ApiKey
		{
			get => this.apiKey;
			private set
			{
				if (!string.Equals(this.apiKey, value))
				{
					this.apiKey = value;
					this.FlagAsDirty(nameof(ApiKey));
				}
			}
		}

		/// <inheritdoc/>
		public string ApiSecret
		{
			get => this.apiSecret;
			private set
			{
				if (!string.Equals(this.apiSecret, value))
				{
					this.apiSecret = value;
					this.FlagAsDirty(nameof(ApiSecret));
				}
			}
		}

		/// <inheritdoc/>
		public string PhoneNumber
		{
			get => this.phoneNumber;
			private set
			{
				if (!string.Equals(this.phoneNumber, value))
				{
					this.phoneNumber = value;
					this.FlagAsDirty(nameof(PhoneNumber));
				}
			}
		}

		/// <inheritdoc/>
		public string Account
		{
			get => this.account;
			private set
			{
				if (!string.Equals(this.account, value))
				{
					this.account = value;
					this.FlagAsDirty(nameof(Account));
				}
			}
		}

		/// <inheritdoc/>
		public string PasswordHash
		{
			get => this.passwordHash;
			private set
			{
				if (!string.Equals(this.passwordHash, value))
				{
					this.passwordHash = value;
					this.FlagAsDirty(nameof(PasswordHash));
				}
			}
		}

		/// <inheritdoc/>
		public string PasswordHashMethod
		{
			get => this.passwordHashMethod;
			private set
			{
				if (!string.Equals(this.passwordHashMethod, value))
				{
					this.passwordHashMethod = value;
					this.FlagAsDirty(nameof(PasswordHashMethod));
				}
			}
		}

		/// <inheritdoc/>
		public string LegalJid
		{
			get => this.legalJid;
			private set
			{
				if (!string.Equals(this.legalJid, value))
				{
					this.legalJid = value;
					this.FlagAsDirty(nameof(LegalJid));
				}
			}
		}

		/// <inheritdoc/>
		public string RegistryJid
		{
			get => this.registryJid;
			private set
			{
				if (!string.Equals(this.registryJid, value))
				{
					this.registryJid = value;
					this.FlagAsDirty(nameof(RegistryJid));
				}
			}
		}

		/// <inheritdoc/>
		public string ProvisioningJid
		{
			get => this.provisioningJid;
			private set
			{
				if (!string.Equals(this.provisioningJid, value))
				{
					this.provisioningJid = value;
					this.FlagAsDirty(nameof(ProvisioningJid));
				}
			}
		}

		/// <inheritdoc/>
		public string HttpFileUploadJid
		{
			get => this.httpFileUploadJid;
			private set
			{
				if (!string.Equals(this.httpFileUploadJid, value))
				{
					this.httpFileUploadJid = value;
					this.FlagAsDirty(nameof(HttpFileUploadJid));
				}
			}
		}

		/// <inheritdoc/>
		public long? HttpFileUploadMaxSize
		{
			get => this.httpFileUploadMaxSize;
			private set
			{
				if (this.httpFileUploadMaxSize != value)
				{
					this.httpFileUploadMaxSize = value;
					this.FlagAsDirty(nameof(HttpFileUploadMaxSize));
				}
			}
		}

		/// <inheritdoc/>
		public string LogJid
		{
			get => this.logJid;
			private set
			{
				if (!string.Equals(this.logJid, value))
				{
					this.logJid = value;
					this.FlagAsDirty(nameof(LogJid));
				}
			}
		}

		/// <inheritdoc/>
		public string MucJid
		{
			get => this.mucJid;
			private set
			{
				if (!string.Equals(this.mucJid, value))
				{
					this.mucJid = value;
					this.FlagAsDirty(nameof(MucJid));
				}
			}
		}

		/// <inheritdoc/>
		public string EDalerJid
		{
			get => this.eDalerJid;
			private set
			{
				if (!string.Equals(this.eDalerJid, value))
				{
					this.eDalerJid = value;
					this.FlagAsDirty(nameof(EDalerJid));
				}
			}
		}

		/// <inheritdoc/>
		public RegistrationStep Step
		{
			get => this.step;
			private set
			{
				if (this.step != value)
				{
					this.step = value;
					this.FlagAsDirty(nameof(Step));
					this.OnStepChanged(EventArgs.Empty);
				}
			}
		}

		/// <inheritdoc/>
		public bool PinIsValid => !this.UsePin || !string.IsNullOrEmpty(this.PinHash);

		/// <inheritdoc/>
		public bool FileUploadIsSupported => !string.IsNullOrWhiteSpace(this.HttpFileUploadJid) && this.HttpFileUploadMaxSize.HasValue;

		/// <inheritdoc/>
		public string Pin
		{
			set => this.pinHash = this.ComputePinHash(value);
		}

		/// <inheritdoc/>
		public string PinHash
		{
			get => this.pinHash;
			private set
			{
				if (!string.Equals(this.pinHash, value))
				{
					this.pinHash = value;
					this.FlagAsDirty(nameof(PinHash));
				}
			}
		}

		/// <inheritdoc/>
		public bool UsePin
		{
			get => this.usePin;
			private set
			{
				if (this.usePin != value)
				{
					this.usePin = value;
					this.FlagAsDirty(nameof(UsePin));
				}
			}
		}

		/// <inheritdoc/>
		public LegalIdentity LegalIdentity
		{
			get => this.legalIdentity;
			private set
			{
				if (!Equals(this.legalIdentity, value))
				{
					this.legalIdentity = value;
					this.FlagAsDirty(nameof(LegalIdentity));
				}
			}
		}

		/// <inheritdoc/>
		public bool IsDirty { get; private set; }

		private void FlagAsDirty(string propertyName)
		{
			this.IsDirty = true;

			if (!this.suppressPropertyChangedEvents)
				this.OnChanged(new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}

		/// <inheritdoc/>
		public void ResetIsDirty()
		{
			this.IsDirty = false;
		}

		#endregion

		#region Build Steps

		private void DecrementConfigurationStep(RegistrationStep? stepToRevertTo = null)
		{
			if (stepToRevertTo.HasValue)
				this.Step = stepToRevertTo.Value;
			else
			{
				switch (this.Step)
				{
					case RegistrationStep.ValidatePhoneNr:
						// Do nothing
						break;

					case RegistrationStep.Account:
						this.Step = RegistrationStep.ValidatePhoneNr;
						break;

					case RegistrationStep.RegisterIdentity:
						this.Step = RegistrationStep.Account;
						break;

					case RegistrationStep.ValidateIdentity:
						this.Step = RegistrationStep.RegisterIdentity;
						break;

					case RegistrationStep.Pin:
						this.Step = RegistrationStep.ValidateIdentity;
						break;
				}
			}
		}

		private void IncrementConfigurationStep(RegistrationStep? stepToGoTo = null)
		{
			if (stepToGoTo.HasValue)
				this.Step = stepToGoTo.Value;
			else
			{
				switch (this.Step)
				{
					case RegistrationStep.ValidatePhoneNr:
						this.Step = RegistrationStep.Account;
						break;

					case RegistrationStep.Account:
						this.Step = RegistrationStep.RegisterIdentity;
						break;

					case RegistrationStep.RegisterIdentity:
						this.Step = RegistrationStep.ValidateIdentity;
						break;

					case RegistrationStep.ValidateIdentity:
						this.Step = RegistrationStep.Pin;
						break;

					case RegistrationStep.Pin:
						this.Step = RegistrationStep.Complete;
						break;
				}
			}
		}

		/// <inheritdoc/>
		public void SetPhone(string PhoneNumber)
		{
			this.PhoneNumber = PhoneNumber;
		}

		/// <inheritdoc/>
		public void SetDomain(string domainName, bool defaultXmppConnectivity, string Key, string Secret)
		{
			this.Domain = domainName;
			this.DefaultXmppConnectivity = defaultXmppConnectivity;
			this.ApiKey = Key;
			this.ApiSecret = Secret;

			if (!string.IsNullOrWhiteSpace(Domain) && Step == RegistrationStep.ValidatePhoneNr)
				this.IncrementConfigurationStep();
		}

		/// <inheritdoc/>
		public void ClearDomain()
		{
			this.Domain = string.Empty;
			this.DecrementConfigurationStep(RegistrationStep.ValidatePhoneNr);
		}

		/// <inheritdoc/>
		public void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod)
		{
			this.Account = accountName;
			this.PasswordHash = clientPasswordHash;
			this.PasswordHashMethod = clientPasswordHashMethod;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;

			if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account)
				this.IncrementConfigurationStep();
		}

		/// <inheritdoc/>
		public void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity identity)
		{
			this.Account = accountName;
			this.PasswordHash = clientPasswordHash;
			this.PasswordHashMethod = clientPasswordHashMethod;
			this.LegalIdentity = identity;
			this.ApiKey = string.Empty;
			this.ApiSecret = string.Empty;

			if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account && !(this.LegalIdentity is null))
			{
				switch (this.LegalIdentity.State)
				{
					case IdentityState.Created:
						this.IncrementConfigurationStep(RegistrationStep.ValidateIdentity);
						break;

					case IdentityState.Approved:
						if (this.usePin && !string.IsNullOrWhiteSpace(this.pinHash))
							this.IncrementConfigurationStep(RegistrationStep.Complete);
						else
							this.IncrementConfigurationStep(RegistrationStep.Pin);
						break;

					default:
						this.IncrementConfigurationStep();
						break;
				}
			}
		}

		/// <inheritdoc/>
		public void ClearAccount()
		{
			this.Account = string.Empty;
			this.PasswordHash = string.Empty;
			this.PasswordHashMethod = string.Empty;
			this.LegalJid = null;

			this.DecrementConfigurationStep(RegistrationStep.ValidatePhoneNr); // prev
		}

		/// <inheritdoc/>
		public void SetLegalIdentity(LegalIdentity Identity)
		{
			this.LegalIdentity = Identity;

			if (this.Step == RegistrationStep.RegisterIdentity && !(Identity is null) &&
				(Identity.State == IdentityState.Created || Identity.State == IdentityState.Approved))
			{
				this.IncrementConfigurationStep();
			}
		}

		/// <inheritdoc/>
		public void ClearLegalIdentity()
		{
			this.LegalIdentity = null;
			this.LegalJid = null;

			this.DecrementConfigurationStep(RegistrationStep.Account); // prev
		}

		/// <inheritdoc/>
		public void RevokeLegalIdentity(LegalIdentity revokedIdentity)
		{
			this.LegalIdentity = revokedIdentity;
			this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
		}

		/// <inheritdoc/>
		public void CompromiseLegalIdentity(LegalIdentity compromisedIdentity)
		{
			this.LegalIdentity = compromisedIdentity;
			this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
		}

		/// <inheritdoc/>
		public void SetIsValidated()
		{
			if (this.Step == RegistrationStep.ValidateIdentity)
				this.IncrementConfigurationStep();
		}

		/// <inheritdoc/>
		public void ClearIsValidated()
		{
			this.LegalIdentity = null;
			this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
		}

		/// <inheritdoc/>
		public void SetPin(string Pin, bool ShouldUsePin)
		{
			this.Pin = Pin;
			this.UsePin = ShouldUsePin;

			if (this.step == RegistrationStep.Pin)
				IncrementConfigurationStep();
		}

		/// <inheritdoc/>
		public void ClearPin()
		{
			this.Pin = string.Empty;
			this.UsePin = false;

			if (this.Step == RegistrationStep.Pin)
				DecrementConfigurationStep(RegistrationStep.ValidateIdentity); // prev
		}

		/// <inheritdoc/>
		public void SetLegalJid(string legalJid)
		{
			this.LegalJid = legalJid;
		}

		/// <inheritdoc/>
		public void SetProvisioningJid(string provisioningJid)
		{
			this.ProvisioningJid = provisioningJid;
		}

		/// <inheritdoc/>
		public void SetRegistryJid(string registryJid)
		{
			this.RegistryJid = registryJid;
		}

		/// <inheritdoc/>
		public void SetFileUploadParameters(string httpFileUploadJid, long? maxSize)
		{
			this.HttpFileUploadJid = httpFileUploadJid;
			this.HttpFileUploadMaxSize = maxSize;
		}

		/// <inheritdoc/>
		public void SetLogJid(string logJid)
		{
			this.LogJid = logJid;
		}

		/// <inheritdoc/>
		public void SetMucJid(string mucJid)
		{
			this.MucJid = mucJid;
		}

		/// <inheritdoc/>
		public void SetEDalerJid(string eDalerJid)
		{
			this.EDalerJid = eDalerJid;
		}

		#endregion

		/// <inheritdoc/>
		public string ComputePinHash(string pin)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(this.objectId);
			sb.Append(':');
			sb.Append(this.domain);
			sb.Append(':');
			sb.Append(this.account);
			sb.Append(':');
			sb.Append(this.legalJid);
			sb.Append(':');
			sb.Append(this.registryJid);
			sb.Append(':');
			sb.Append(this.provisioningJid);
			sb.Append(':');
			sb.Append(pin);

			string s = sb.ToString();
			byte[] data = Encoding.UTF8.GetBytes(s);

			return Hashes.ComputeSHA384HashString(data);
		}

		/// <summary>
		/// Clears the entire profile.
		/// </summary>
		public void ClearAll()
		{
			this.legalIdentity = null;
			this.domain = string.Empty;
			this.apiKey = string.Empty;
			this.apiSecret = string.Empty;
			this.phoneNumber = string.Empty;
			this.account = string.Empty;
			this.passwordHash = string.Empty;
			this.passwordHashMethod = string.Empty;
			this.legalJid = string.Empty;
			this.registryJid = string.Empty;
			this.provisioningJid = string.Empty;
			this.httpFileUploadJid = string.Empty;
			this.logJid = string.Empty;
			this.mucJid = string.Empty;
			this.eDalerJid = string.Empty;
			this.pinHash = string.Empty;
			this.httpFileUploadMaxSize = null;
			this.usePin = false;
			this.step = RegistrationStep.ValidatePhoneNr;
			this.defaultXmppConnectivity = false;

			this.IsDirty = true;
		}

	}
}