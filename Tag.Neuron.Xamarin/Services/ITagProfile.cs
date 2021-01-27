using System;
using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// The TAG Profile is the heart of the digital identity for a specific user/device.
    /// Use this instance to add and make a profile more complete.
    /// The TAG Profile holds relevant data connected to not only where the user is in the registraton process,
    /// but also Xmpp identifiers.
    /// </summary>
    public interface ITagProfile
    {
        /// <summary>
        /// An event that triggers during the registration/profile build process, as the profile becomes more/less complete.
        /// </summary>
        event EventHandler StepChanged;
        event PropertyChangedEventHandler Changed;
        string Domain { get; }
        string Account { get; }
        string PasswordHash { get; }
        string PasswordHashMethod { get; }
        string LegalJid { get; }
        string RegistryJid { get; }
        string ProvisioningJid { get; }
        string HttpFileUploadJid { get; }
        long? HttpFileUploadMaxSize { get; }
        string LogJid { get; }
        string MucJid { get; }
        RegistrationStep Step { get; }
        bool PinIsValid { get; }
        bool FileUploadIsSupported { get; }
        string Pin { set; }
        string PinHash { get; }
        bool UsePin { get; }
        LegalIdentity LegalIdentity { get; }
        string[] Domains { get; }
        bool IsDirty { get; }
        TagConfiguration ToConfiguration();
        void FromConfiguration(TagConfiguration configuration);
        bool NeedsUpdating();
        bool LegalIdentityNeedsUpdating();
        bool IsCompleteOrWaitingForValidation();
        bool IsComplete();
        void ResetIsDirty();
        void SetDomain(string domainName);
        void ClearDomain();
        void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod);
        void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity legalIdentity);
        void ClearAccount();
        void SetLegalIdentity(LegalIdentity legalIdentity);
        void ClearLegalIdentity();
        void RevokeLegalIdentity(LegalIdentity revokedIdentity);
        void CompromiseLegalIdentity(LegalIdentity compromisedIdentity);
        void SetIsValidated();
        void ClearIsValidated();
        void SetPin(string pin, bool usePin);
        void ClearPin();
        void SetLegalJId(string legalJId);
        void SetProvisioningJId(string provisioningJId);
        void SetRegistryJId(string registryJId);
        void SetFileUploadParameters(string httpFileUploadJId, long? maxSize);
        void SetLogJId(string logJId);
        void SetMucJId(string mucJId);
        string ComputePinHash(string pin);
        bool TryGetKeys(string domainName, out string apiKey, out string secret);
    }
}