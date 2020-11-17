using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Networking.DNS;
using Waher.Networking.DNS.ResourceRecords;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;
using Waher.Security;

namespace XamarinApp.Services
{
    public partial class TagServiceSettings
    {
        private const int DefaultPortNumber = 5222;
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
        private int step = 0;

        protected virtual void OnChanged(EventArgs e)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void FlagAsDirty()
        {
            IsDirty = true;
            OnChanged(EventArgs.Empty);
        }

        public bool LegalIdentityIsValid => this.LegalIdentity != null &&
                                            this.LegalIdentity.State != IdentityState.Compromised &&
                                            this.LegalIdentity.State != IdentityState.Obsoleted &&
                                            this.LegalIdentity.State != IdentityState.Rejected;

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

        public void CloneFrom(TagServiceSettings other)
        {
            if (other == null)
            {
                return;
            }

            this.ObjectId = other.ObjectId;
            this.Step = other.Step;
            this.Domain = other.Domain;
            this.Account = other.Account;
            this.PasswordHash = other.PasswordHash;
            this.PasswordHashMethod = other.PasswordHashMethod;
            this.LegalJid = other.LegalJid;
            this.RegistryJid = other.RegistryJid;
            this.ProvisioningJid = other.ProvisioningJid;
            this.HttpFileUploadJid = other.HttpFileUploadJid;
            this.HttpFileUploadMaxSize = other.HttpFileUploadMaxSize;
            this.PinHash = other.PinHash;
            this.UsePin = other.UsePin;

            if (other.LegalIdentity != null)
            {
                StringBuilder xml = new StringBuilder();
                other.LegalIdentity.Serialize(xml, true, true, true, true, true, true, true);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml.ToString());    
                this.LegalIdentity = LegalIdentity.Parse(doc.DocumentElement);
            }
        }

        public void SetPin(string pin, bool usePin)
        {
            Pin = pin;
            UsePin = usePin;
        }

        public void ResetPin()
        {
            Pin = string.Empty;
            UsePin = false;
        }

        public bool PinIsValid => !this.UsePin || !string.IsNullOrEmpty(this.PinHash);

        public void SetAccount(string accountNameText, string clientPasswordHash, string clientPasswordHashMethod)
        {
            Account = accountNameText;
            PasswordHash = clientPasswordHash;
            PasswordHashMethod = clientPasswordHashMethod;
        }

        public void SetDomain(string domainName, string legalJid)
        {
            Domain = domainName;
            LegalJid = legalJid;
        }

        public bool FileUploadIsSupported =>
            !string.IsNullOrEmpty(this.HttpFileUploadJid) &&
            this.HttpFileUploadMaxSize.HasValue;

        [ObjectId]
        public string ObjectId
        {
            get => this.objectId;
            set
            {
                if (!string.Equals(this.objectId, value))
                {
                    this.objectId = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string Domain
        {
            get => this.domain;
            set
            {
                if (!string.Equals(this.domain, value))
                {
                    this.domain = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string Account
        {
            get => this.account;
            set
            {
                if (!string.Equals(this.account, value))
                {
                    this.account = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string PasswordHash
        {
            get => this.passwordHash;
            set
            {
                if (!string.Equals(this.account, value))
                {
                    this.passwordHash = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string PasswordHashMethod
        {
            get => this.passwordHashMethod;
            set
            {
                if (!string.Equals(this.passwordHashMethod, value))
                {
                    this.passwordHashMethod = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string LegalJid
        {
            get => this.legalJid;
            set
            {
                if (!string.Equals(this.legalJid, value))
                {
                    this.legalJid = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string RegistryJid
        {
            get => this.registryJid;
            set
            {
                if (!string.Equals(this.registryJid, value))
                {
                    this.registryJid = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string ProvisioningJid
        {
            get => this.provisioningJid;
            set
            {
                if (!string.Equals(this.provisioningJid, value))
                {
                    this.provisioningJid = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueStringEmpty]
        public string HttpFileUploadJid
        {
            get => this.httpFileUploadJid;
            set
            {
                if (!string.Equals(this.httpFileUploadJid, value))
                {
                    this.httpFileUploadJid = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueNull]
        public long? HttpFileUploadMaxSize
        {
            get => this.httpFileUploadMaxSize;
            set
            {
                if (this.httpFileUploadMaxSize != value)
                {
                    this.httpFileUploadMaxSize = value;
                    FlagAsDirty();
                }
            }
        }

        public int Step
        {
            get => this.step;
            private set
            {
                if (this.step != value && value >= 0 && value <= 5)
                {
                    this.step = value;
                    FlagAsDirty();
                }
            }
        }

        public void DecrementConfigurationStep(int? stepToRevertTo = null)
        {
            if (stepToRevertTo.HasValue)
                Step = stepToRevertTo.Value;
            else
                Step--;
        }

        public void IncrementConfigurationStep()
        {
            Step++;
        }


        public string Pin
        {
            set => this.pinHash = this.ComputePinHash(value);
        }

        public string ComputePinHash(string Pin)
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
            sb.Append(Pin);

            byte[] data = Encoding.UTF8.GetBytes(sb.ToString());

            return Hashes.ComputeSHA384HashString(data);
        }

        [DefaultValueStringEmpty]
        public string PinHash
        {
            get => this.pinHash;
            set
            {
                if (!string.Equals(this.pinHash, value))
                {
                    this.pinHash = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValue(false)]
        public bool UsePin
        {
            get => this.usePin;
            set
            {
                if (this.usePin != value)
                {
                    this.usePin = value;
                    FlagAsDirty();
                }
            }
        }

        [DefaultValueNull]
        public LegalIdentity LegalIdentity
        {
            get => this.legalIdentity;
            set
            {
                if (!Equals(this.legalIdentity, value))
                {
                    this.legalIdentity = value;
                    FlagAsDirty();
                }
            }
        }

        public string[] Domains => clp.Keys.ToArray();

        public bool HasLegalIdentityAttachments => this.LegalIdentity.Attachments != null;

        public Attachment[] GetLegalIdentityAttachments()
        {
            return this.LegalIdentity?.Attachments;
        }

        public bool TryGetKeys(string domainName, out string apiKey, out string secret)
        {
            if (clp.TryGetValue(domainName, out KeyValuePair<string, string> P))
            {
                apiKey = P.Key;
                secret = P.Value;
                return true;
            }
            apiKey = secret = null;
            return false;
        }

        [DefaultValue(false)]
        public bool IsDirty { get; set; }
    }
}