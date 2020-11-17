using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Waher.Networking.XMPP.Contracts;
using Waher.Persistence.Attributes;
using Waher.Security;

namespace Waher.IoTGateway.Setup
{
    [CollectionName("Configuration")]
    public partial class XmppConfiguration
    {
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
        private int step = 0;
        private bool usePin = false;

        [ObjectId]
        public string ObjectId
        {
            get => this.objectId;
            set => this.objectId = value;
        }

        [DefaultValueStringEmpty]
        public string Domain
        {
            get => this.domain;
            set => this.domain = value;
        }

        [DefaultValueStringEmpty]
        public string Account
        {
            get => this.account;
            set => this.account = value;
        }

        [DefaultValueStringEmpty]
        public string PasswordHash
        {
            get => this.passwordHash;
            set => this.passwordHash = value;
        }

        [DefaultValueStringEmpty]
        public string PasswordHashMethod
        {
            get => this.passwordHashMethod;
            set => this.passwordHashMethod = value;
        }

        [DefaultValue(0)]
        public int Step
        {
            get => this.step;
            set => this.step = value;
        }

        [DefaultValueStringEmpty]
        public string LegalJid
        {
            get => this.legalJid;
            set => this.legalJid = value;
        }

        [DefaultValueStringEmpty]
        public string RegistryJid
        {
            get => this.registryJid;
            set => this.registryJid = value;
        }

        [DefaultValueStringEmpty]
        public string ProvisioningJid
        {
            get => this.provisioningJid;
            set => this.provisioningJid = value;
        }

        [DefaultValueStringEmpty]
        public string HttpFileUploadJid
		{
            get => this.httpFileUploadJid;
            set => this.httpFileUploadJid = value;
        }

        [DefaultValueNull]
        public long? HttpFileUploadMaxSize
        {
            get => this.httpFileUploadMaxSize;
            set => this.httpFileUploadMaxSize = value;
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

            byte[] Data = Encoding.UTF8.GetBytes(sb.ToString());

            return Hashes.ComputeSHA384HashString(Data);
        }

        [DefaultValueStringEmpty]
        public string PinHash
		{
            get => this.pinHash;
            set => this.pinHash = value;
		}

        [DefaultValue(false)]
        public bool UsePin
        {
            get => this.usePin;
            set => this.usePin = value;
        }

        [DefaultValueNull]
        public LegalIdentity LegalIdentity
        {
            get => this.legalIdentity;
            set => this.legalIdentity = value;
        }

        public static string[] Domains
        {
            get
            {
                string[] Result = new string[clp.Count];
                clp.Keys.CopyTo(Result, 0);
                return Result;
            }
        }

        internal static bool TryGetKeys(string Domain, out string ApiKey, out string Secret)
        {
            if (clp.TryGetValue(Domain, out KeyValuePair<string, string> P))
            {
                ApiKey = P.Key;
                Secret = P.Value;
                return true;
            }
            else
            {
                ApiKey = Secret = null;
                return false;
            }
        }

    }
}
