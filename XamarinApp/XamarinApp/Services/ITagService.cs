using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.IoTGateway.Setup;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;

namespace XamarinApp.Services
{
    public interface ITagService : IDisposable
    {
        Task Init();

        bool IsOnline { get; }

        XmppClient Xmpp { get; }

        ContractsClient Contracts { get; }

        event EventHandler<LegalIdentityChangedEventArgs> LegalIdentityChanged;

        Task AddLegalIdentity(List<Property> properties, params LegalIdentityAttachment[] attachments);

        Task<LegalIdentity[]> GetLegalIdentitiesAsync();

        bool HasLegalIdentityAttachments { get; }
        Attachment[] GetLegalIdentityAttachments();

        Task<KeyValuePair<string, TemporaryFile>> GetAttachmentAsync(string url);
        Task<KeyValuePair<string, TemporaryFile>> GetAttachmentAsync(string url, TimeSpan timeout);

        XmppConfiguration Configuration { get; }

        bool FileUploadIsSupported { get; }

        bool LegalIdentityIsValid { get; }

        bool PinIsValid { get; }

        Task UpdateXmpp();

        Task<bool> CheckServices();

        Task<bool> FindServices(XmppClient client);

        string CreateRandomPassword();

        Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName);

        void UpdateConfiguration();
    }
}