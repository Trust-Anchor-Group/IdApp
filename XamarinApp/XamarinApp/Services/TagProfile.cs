using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;
using Waher.Security;
using XamarinApp.Extensions;

namespace XamarinApp.Services
{
    public partial class TagProfile
    {
        public event EventHandler StepChanged;
        public event EventHandler Changed;
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
        private string pinHash;
        private long? httpFileUploadMaxSize;
        private bool usePin;
        private RegistrationStep step = RegistrationStep.Operator;

        protected virtual void OnStepChanged(EventArgs e)
        {
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnChanged(EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
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
                PinHash = this.PinHash,
                UsePin = this.UsePin,
                LegalIdentity = this.LegalIdentity,
                Step = this.Step
            };
            return clone;
        }

        public void FromConfiguration(TagConfiguration configuration)
        {
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
            this.PinHash = configuration.PinHash;
            this.UsePin = configuration.UsePin;
            this.LegalIdentity = configuration.LegalIdentity;
            // Do this last, as listeners will read the other properties when the event is fired.
            this.Step = configuration.Step;
        }

        public bool NeedsUpdating()
        {
            return string.IsNullOrWhiteSpace(this.LegalJid) ||
                   string.IsNullOrWhiteSpace(this.RegistryJid) ||
                   string.IsNullOrWhiteSpace(this.ProvisioningJid) ||
                   string.IsNullOrWhiteSpace(this.HttpFileUploadJid) ||
                   string.IsNullOrWhiteSpace(this.LogJid);
        }

        public bool LegalIdentityNeedsUpdating()
        {
            return this.legalIdentity.NeedsUpdating();
        }

        public bool IsCompleteOrWaitingForValidation()
        {
            return this.Step >= RegistrationStep.ValidateIdentity;
        }

        public bool IsComplete()
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.FlagAsDirty();
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
                    this.IsDirty = true;
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
                    this.IsDirty = true;
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
                    this.IsDirty = true;
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
                    this.IsDirty = true;
                }
            }
        }

        public string[] Domains => this.clp.Keys.ToArray();

        [DefaultValue(false)]
        public bool IsDirty { get; private set; }

        private void FlagAsDirty()
        {
            this.IsDirty = true;
            this.OnChanged(EventArgs.Empty);
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
        public void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity legalIdentity)
        {
            this.Account = accountName;
            this.PasswordHash = clientPasswordHash;
            this.PasswordHashMethod = clientPasswordHashMethod;
            this.LegalIdentity = legalIdentity;
            if (!string.IsNullOrWhiteSpace(this.Account) && Step == RegistrationStep.Account)
            {
                if (legalIdentity.IsCreatedOrApproved())
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
        public void SetLegalIdentity(LegalIdentity legalIdentity)
        {
            this.LegalIdentity = legalIdentity;
            if (this.Step == RegistrationStep.RegisterIdentity)
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
        }

        // Step 5
        public void SetPin(string pin, bool usePin)
        {
            this.Pin = pin;
            this.UsePin = usePin;
            IncrementConfigurationStep();
        }

        public void ClearPin()
        {
            this.Pin = string.Empty;
            this.UsePin = false;
            DecrementConfigurationStep(RegistrationStep.ValidateIdentity); // prev
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
        public string PinHash { get; set; }

        [DefaultValue(false)]
        public bool UsePin { get; set; }

        [DefaultValueNull]
        public LegalIdentity LegalIdentity { get; set; }

        [DefaultValue(RegistrationStep.Operator)]
        public RegistrationStep Step { get; set; }
    }
}