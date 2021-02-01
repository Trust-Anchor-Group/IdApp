using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Models;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;
using Waher.Security;

namespace Tag.Neuron.Xamarin.Services
{
    public class TagProfile : ITagProfile
    {
        private readonly Dictionary<string, KeyValuePair<string, string>> domains;
        public event EventHandler StepChanged;
        public event PropertyChangedEventHandler Changed;
        private LegalIdentity legalIdentity;
        private string objectId;
        private string domain;
        private string account;
        private string passwordHash;
        private string passwordHashMethod;
        private string legalJid;
        private string registryJid;
        private string provisioningJid;
        private string httpFileUploadJid;
        private string logJid;
        private string mucJid;
        private string pinHash;
        private long? httpFileUploadMaxSize;
        private bool usePin;
        private RegistrationStep step = RegistrationStep.Operator;
        private bool suppressPropertyChangedEvents;

        public TagProfile(params DomainModel[] domainModels)
        {
            this.domains = new Dictionary<string, KeyValuePair<string, string>>(StringComparer.CurrentCultureIgnoreCase);
            if (domainModels != null && domainModels.Length > 0)
            {
                foreach (DomainModel domainModel in domainModels)
                {
                    this.domains[domainModel.Name] = new KeyValuePair<string, string>(domainModel.Key, domainModel.Secret);
                }
            }
        }

        protected virtual void OnStepChanged(EventArgs e)
        {
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnChanged(PropertyChangedEventArgs e)
        {
            Changed?.Invoke(this, e);
        }

        public TagConfiguration ToConfiguration()
        {
            TagConfiguration clone = new TagConfiguration
            {
                ObjectId = this.objectId,
                Domain = this.Domain,
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
                PinHash = this.PinHash,
                UsePin = this.UsePin,
                LegalIdentity = this.LegalIdentity,
                Step = this.Step
            };
            return clone;
        }

        public void FromConfiguration(TagConfiguration configuration)
        {
            try
            {
                this.suppressPropertyChangedEvents = true;
                this.objectId = configuration.ObjectId;
                this.Domain = configuration.Domain;
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

        public virtual bool NeedsUpdating()
        {
            return string.IsNullOrWhiteSpace(this.LegalJid) ||
                   string.IsNullOrWhiteSpace(this.RegistryJid) ||
                   string.IsNullOrWhiteSpace(this.ProvisioningJid) ||
                   string.IsNullOrWhiteSpace(this.HttpFileUploadJid) ||
                   string.IsNullOrWhiteSpace(this.LogJid) ||
                   string.IsNullOrWhiteSpace(this.MucJid);
        }

        public virtual bool LegalIdentityNeedsUpdating()
        {
            return this.legalIdentity.NeedsUpdating();
        }

        public virtual bool IsCompleteOrWaitingForValidation()
        {
            return this.Step >= RegistrationStep.ValidateIdentity;
        }

        public virtual bool IsComplete()
        {
            return this.Step == RegistrationStep.Complete;
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
                    this.FlagAsDirty(nameof(Domain));
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
                    this.FlagAsDirty(nameof(Account));
                }
            }
        }

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

        public bool PinIsValid => !this.UsePin || !string.IsNullOrEmpty(this.PinHash);

        public bool FileUploadIsSupported => !string.IsNullOrWhiteSpace(this.HttpFileUploadJid) && this.HttpFileUploadMaxSize.HasValue;

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
                    this.FlagAsDirty(nameof(PinHash));
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
                    this.FlagAsDirty(nameof(UsePin));
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
                    this.FlagAsDirty(nameof(LegalIdentity));
                }
            }
        }

        public string[] Domains => this.domains.Keys.ToArray();

        public bool IsDirty { get; private set; }

        private void FlagAsDirty(string propertyName)
        {
            this.IsDirty = true;
            if (!this.suppressPropertyChangedEvents)
            {
                this.OnChanged(new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ResetIsDirty()
        {
            this.IsDirty = false;
        }

        #endregion

        #region Build Steps

        private void DecrementConfigurationStep(RegistrationStep? stepToRevertTo = null)
        {
            if (stepToRevertTo.HasValue)
            {
                this.Step = stepToRevertTo.Value;
            }
            else
            {
                switch (this.Step)
                {
                    case RegistrationStep.Operator:
                        // Do nothing
                        break;
                    case RegistrationStep.Account:
                        this.Step = RegistrationStep.Operator;
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
            {
                this.Step = stepToGoTo.Value;
            }
            else
            {
                switch (this.Step)
                {
                    case RegistrationStep.Operator:
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

        // Step 1
        public void SetDomain(string domainName)
        {
            this.Domain = domainName;
            if (!string.IsNullOrWhiteSpace(Domain) && Step == RegistrationStep.Operator)
            {
                this.IncrementConfigurationStep();
            }
        }

        public void ClearDomain()
        {
            this.Domain = string.Empty;
            this.DecrementConfigurationStep(RegistrationStep.Operator);
        }

        // Step 2
        public void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod)
        {
            this.Account = accountName;
            this.PasswordHash = clientPasswordHash;
            this.PasswordHashMethod = clientPasswordHashMethod;
            if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account)
            {
                this.IncrementConfigurationStep();
            }
        }

        // Step 2, 3
        public void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity identity)
        {
            this.Account = accountName;
            this.PasswordHash = clientPasswordHash;
            this.PasswordHashMethod = clientPasswordHashMethod;
            this.LegalIdentity = identity;
            if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account && this.LegalIdentity != null)
            {
                if (this.LegalIdentity.IsCreatedOrApproved())
                {
                    this.IncrementConfigurationStep(RegistrationStep.ValidateIdentity);
                }
                else
                {
                    this.IncrementConfigurationStep();
                }
            }
        }

        public void ClearAccount()
        {
            this.Account = string.Empty;
            this.PasswordHash = string.Empty;
            this.PasswordHashMethod = string.Empty;
            this.DecrementConfigurationStep(RegistrationStep.Operator); // prev
        }

        // Step 3
        public void SetLegalIdentity(LegalIdentity identity)
        {
            this.LegalIdentity = identity;
            if (this.Step == RegistrationStep.RegisterIdentity && this.LegalIdentity.IsCreatedOrApproved())
            {
                this.IncrementConfigurationStep();
            }
        }

        public void ClearLegalIdentity()
        {
            this.LegalIdentity = null;
            this.LegalJid = null;
            this.DecrementConfigurationStep(RegistrationStep.Account); // prev
        }

        public void RevokeLegalIdentity(LegalIdentity revokedIdentity)
        {
            this.LegalIdentity = revokedIdentity;
            this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
        }

        public void CompromiseLegalIdentity(LegalIdentity compromisedIdentity)
        {
            this.LegalIdentity = compromisedIdentity;
            this.DecrementConfigurationStep(RegistrationStep.RegisterIdentity);
        }

        // Step 4
        public void SetIsValidated()
        {
            if (this.Step == RegistrationStep.ValidateIdentity)
            {
                this.IncrementConfigurationStep();
            }
        }

        public void ClearIsValidated()
        {
            this.DecrementConfigurationStep(RegistrationStep.Account); // Bypass Register (step 3), go directly to Account (Step2)
            this.ClearLegalIdentity();
        }

        // Step 5
        public void SetPin(string pin, bool shouldUsePin)
        {
            this.Pin = pin;
            this.UsePin = shouldUsePin;
            if (this.step == RegistrationStep.Pin)
            {
                IncrementConfigurationStep();
            }
        }

        public void ClearPin()
        {
            this.Pin = string.Empty;
            this.UsePin = false;
            if (this.Step == RegistrationStep.Pin)
            {
                DecrementConfigurationStep(RegistrationStep.ValidateIdentity); // prev
            }
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

        public void SetFileUploadParameters(string httpFileUploadJId, long? maxSize)
        {
            this.HttpFileUploadJid = httpFileUploadJId;
            this.HttpFileUploadMaxSize = maxSize;
        }

        public void SetLogJId(string logJId)
        {
            this.LogJid = logJId;
        }

        public void SetMucJId(string mucJId)
        {
            this.MucJid = mucJId;
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
            if (domains.TryGetValue(domainName, out KeyValuePair<string, string> entry))
            {
                apiKey = entry.Key;
                secret = entry.Value;
                return true;
            }
            apiKey = secret = null;
            return false;
        }
    }

    public enum RegistrationStep
    {
        Operator = 0,
        Account = 1,
        RegisterIdentity = 2,
        ValidateIdentity = 3,
        Pin = 4,
        Complete = 5
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
        public string LogJid { get; set; }

        [DefaultValueStringEmpty]
        public string MucJid { get; set; }

        [DefaultValueStringEmpty]
        public string PinHash { get; set; }

        [Waher.Persistence.Attributes.DefaultValue(false)]
        public bool UsePin { get; set; }

        [DefaultValueNull]
        public LegalIdentity LegalIdentity { get; set; }

        [Waher.Persistence.Attributes.DefaultValue(RegistrationStep.Operator)]
        public RegistrationStep Step { get; set; }
    }
}