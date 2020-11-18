using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;
using Waher.Security;
using XamarinApp.Extensions;

namespace XamarinApp.Services
{
    public partial class TagProfile
    {
        private const int DefaultPortNumber = 5222;
        public event EventHandler StepChanged;
        public event EventHandler Changed;

        private LegalIdentity legalIdentity = null;
        private string objectId = null;
        private string domain = string.Empty;
        private string account = string.Empty;
        private string passwordHash = string.Empty;
        private string passwordHashMethod = string.Empty;
        private string legalJid = string.Empty;
        private string registryJid = string.Empty;
        private string provisioningJid = string.Empty;
        private string httpFileUploadJid = string.Empty;
        private string pinHash = string.Empty;
        private long? httpFileUploadMaxSize = null;
        private bool usePin = false;
        private RegistrationStep step = 0;

        protected virtual void OnStepChanged(EventArgs e)
        {
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnChanged(EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public async Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName = null)
        {
            domainName = domainName ?? Domain;

            try
            {
                SRV SRV = await DnsResolver.LookupServiceEndpoint(domainName, "xmpp-client", "tcp");
                if (!(SRV is null) && !string.IsNullOrEmpty(SRV.TargetHost) && SRV.Port > 0)
                    return (SRV.TargetHost, SRV.Port);
            }
            catch (Exception)
            {
                // No service endpoint registered
            }

            return (domainName, DefaultPortNumber);
        }

        public TagConfiguration ToConfiguration()
        {
            TagConfiguration clone = new TagConfiguration();
            clone.ObjectId = this.objectId;
            clone.Step = this.Step;
            clone.Domain = this.Domain;
            clone.Account = this.Account;
            clone.PasswordHash = this.PasswordHash;
            clone.PasswordHashMethod = this.PasswordHashMethod;
            clone.LegalJid = this.LegalJid;
            clone.RegistryJid = this.RegistryJid;
            clone.ProvisioningJid = this.ProvisioningJid;
            clone.HttpFileUploadJid = this.HttpFileUploadJid;
            clone.HttpFileUploadMaxSize = this.HttpFileUploadMaxSize;
            clone.PinHash = this.PinHash;
            clone.UsePin = this.UsePin;
            clone.LegalIdentity = this.LegalIdentity;
            return clone;
        }

        public void FromConfiguration(TagConfiguration configuration)
        {
            this.objectId = configuration.ObjectId;
            this.Step = configuration.Step;
            this.Domain = configuration.Domain;
            this.Account = configuration.Account;
            this.PasswordHash = configuration.PasswordHash;
            this.PasswordHashMethod = configuration.PasswordHashMethod;
            this.LegalJid = configuration.LegalJid;
            this.RegistryJid = configuration.RegistryJid;
            this.ProvisioningJid = configuration.ProvisioningJid;
            this.HttpFileUploadJid = configuration.HttpFileUploadJid;
            this.HttpFileUploadMaxSize = configuration.HttpFileUploadMaxSize;
            this.PinHash = configuration.PinHash;
            this.UsePin = configuration.UsePin;
            this.LegalIdentity = configuration.LegalIdentity;
        }

        public bool IsComplete()
        {
            return Step == RegistrationStep.Pin && PinIsValid;
        }

        #region Properties

        public string Domain
        {
            get => this.domain;
            private set
            {
                if (!string.Equals(this.domain, value))
                {
                    this.domain = value;
                    FlagAsDirty();
                }
            }
        }

        public string Account
        {
            get => this.account;
            private set
            {
                if (!string.Equals(this.account, value))
                {
                    this.account = value;
                    FlagAsDirty();
                }
            }
        }

        public string PasswordHash
        {
            get => this.passwordHash;
            private set
            {
                if (!string.Equals(this.account, value))
                {
                    this.passwordHash = value;
                    FlagAsDirty();
                }
            }
        }

        public string PasswordHashMethod
        {
            get => this.passwordHashMethod;
            private set
            {
                if (!string.Equals(this.passwordHashMethod, value))
                {
                    this.passwordHashMethod = value;
                    FlagAsDirty();
                }
            }
        }

        public string LegalJid
        {
            get => this.legalJid;
            private set
            {
                if (!string.Equals(this.legalJid, value))
                {
                    this.legalJid = value;
                    FlagAsDirty();
                }
            }
        }

        public string RegistryJid
        {
            get => this.registryJid;
            private set
            {
                if (!string.Equals(this.registryJid, value))
                {
                    this.registryJid = value;
                    FlagAsDirty();
                }
            }
        }

        public string ProvisioningJid
        {
            get => this.provisioningJid;
            private set
            {
                if (!string.Equals(this.provisioningJid, value))
                {
                    this.provisioningJid = value;
                    FlagAsDirty();
                }
            }
        }

        public string HttpFileUploadJid
        {
            get => this.httpFileUploadJid;
            private set
            {
                if (!string.Equals(this.httpFileUploadJid, value))
                {
                    this.httpFileUploadJid = value;
                    FlagAsDirty();
                }
            }
        }

        public long? HttpFileUploadMaxSize
        {
            get => this.httpFileUploadMaxSize;
            private set
            {
                if (this.httpFileUploadMaxSize != value)
                {
                    this.httpFileUploadMaxSize = value;
                    FlagAsDirty();
                }
            }
        }

        public RegistrationStep Step
        {
            get => this.step;
            private set
            {
                if (this.step != value)
                {
                    this.step = value;
                    IsDirty = true;
                    OnStepChanged(EventArgs.Empty);
                }
            }
        }

        public bool PinIsValid => !this.UsePin || !string.IsNullOrEmpty(this.PinHash);

        public bool FileUploadIsSupported =>
            !string.IsNullOrEmpty(this.HttpFileUploadJid) &&
            this.HttpFileUploadMaxSize.HasValue;

        public string Pin
        {
            set => this.pinHash = this.ComputePinHash(value);
        }

        public string PinHash
        {
            get => this.pinHash;
            private set
            {
                if (!string.Equals(this.pinHash, value))
                {
                    this.pinHash = value;
                    IsDirty = true;
                }
            }
        }

        public bool UsePin
        {
            get => this.usePin;
            private set
            {
                if (this.usePin != value)
                {
                    this.usePin = value;
                    IsDirty = true;
                }
            }
        }

        public LegalIdentity LegalIdentity
        {
            get => this.legalIdentity;
            private set
            {
                if (!Equals(this.legalIdentity, value))
                {
                    this.legalIdentity = value;
                    IsDirty = true;
                }
            }
        }

        public string[] Domains => clp.Keys.ToArray();

        [DefaultValue(false)]
        public bool IsDirty { get; private set; }

        private void FlagAsDirty()
        {
            IsDirty = true;
            OnChanged(EventArgs.Empty);
        }

        public void ResetIsDirty()
        {
            IsDirty = false;
        }

        #endregion

        #region Build Steps

        private void DecrementConfigurationStep(RegistrationStep? stepToRevertTo = null)
        {
            if (stepToRevertTo.HasValue)
            {
                Step = stepToRevertTo.Value;
            }
            else
            {
                switch (Step)
                {
                    case RegistrationStep.Operator:
                        // Do nothing
                        break;
                    case RegistrationStep.Account:
                        Step = RegistrationStep.Operator;
                        break;
                    case RegistrationStep.RegisterIdentity:
                        Step = RegistrationStep.Account;
                        break;
                    case RegistrationStep.Identity:
                        Step = RegistrationStep.RegisterIdentity;
                        break;
                    case RegistrationStep.Pin:
                        Step = RegistrationStep.Identity;
                        break;
                }
            }
        }

        private void IncrementConfigurationStep()
        {
            switch (Step)
            {
                case RegistrationStep.Operator:
                    Step = RegistrationStep.Account;
                    break;
                case RegistrationStep.Account:
                    Step = RegistrationStep.RegisterIdentity;
                    break;
                case RegistrationStep.RegisterIdentity:
                    Step = RegistrationStep.Identity;
                    break;
                case RegistrationStep.Identity:
                    Step = RegistrationStep.Pin;
                    break;
                case RegistrationStep.Pin:
                    // Do nothing
                    break;
            }
        }

        // Step 1
        public void SetDomain(string domainName, string legalJid)
        {
            this.Domain = domainName;
            this.LegalJid = legalJid;
            if (!string.IsNullOrWhiteSpace(Domain) && Step == RegistrationStep.Operator)
            {
                IncrementConfigurationStep();
            }
        }

        // Step 2
        public void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod)
        {
            this.Account = accountName;
            this.PasswordHash = clientPasswordHash;
            this.PasswordHashMethod = clientPasswordHashMethod;
            if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account)
            {
                IncrementConfigurationStep();
            }
        }

        // Step 3
        public void SetLegalIdentity(LegalIdentity legalIdentity)
        {
            this.legalIdentity = legalIdentity;
            if (this.legalIdentity.IsValid() && Step == RegistrationStep.RegisterIdentity)
            {
                IncrementConfigurationStep();
            }
        }

        public void SetPin(string pin, bool usePin)
        {
            this.Pin = pin;
            this.UsePin = usePin;
        }

        public void ResetPin()
        {
            this.Pin = string.Empty;
            this.UsePin = false;
        }

        public void SetLegalJId(string legalJId)
        {
            this.LegalJid = legalJId;
        }

        public void SetProvisioningJId(string provisioningJId)
        {
            this.ProvisioningJid = provisioningJId;
        }

        public void SetRegistryJId(string registryJId)
        {
            this.RegistryJid = registryJId;
        }

        public void SetHttpParameters(string httpFileUploadJId, long? maxSize)
        {
            this.HttpFileUploadJid = httpFileUploadJId;
            this.HttpFileUploadMaxSize = maxSize;
        }

        #endregion

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

            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());

            return Hashes.ComputeSHA384HashString(data);
        }

        public bool TryGetKeys(string domainName, out string apiKey, out string secret)
        {
            if (clp.TryGetValue(domainName, out KeyValuePair<string, string> entry))
            {
                apiKey = entry.Key;
                secret = entry.Value;
                return true;
            }
            apiKey = secret = null;
            return false;
        }
    }

    public enum RegistrationStep : int
    {
        Operator = 1,
        Account = 2,
        RegisterIdentity = 3,
        Identity = 4,
        Pin = 5
    }

    [CollectionName("Configuration")]
    public sealed class TagConfiguration
    {
        [ObjectId]
        public string ObjectId { get; set; }

        [DefaultValueStringEmpty]
        public string Domain { get; set; }

        [DefaultValueStringEmpty]
        public string Account { get; set; }

        [DefaultValueStringEmpty]
        public string PasswordHash { get; set; }

        [DefaultValueStringEmpty]
        public string PasswordHashMethod { get; set; }

        [DefaultValueStringEmpty]
        public string LegalJid { get; set; }

        [DefaultValueStringEmpty]
        public string RegistryJid { get; set; }

        [DefaultValueStringEmpty]
        public string ProvisioningJid { get; set; }

        [DefaultValueStringEmpty]
        public string HttpFileUploadJid { get; set; }

        [DefaultValueNull]
        public long? HttpFileUploadMaxSize { get; set; }

        [DefaultValueStringEmpty]
        public string PinHash { get; set; }

        [DefaultValue(false)]
        public bool UsePin { get; set; }

        [DefaultValueNull]
        public LegalIdentity LegalIdentity { get; set; }

        [DefaultValue(RegistrationStep.Operator)]
        public RegistrationStep Step { get; set; }
    }
}