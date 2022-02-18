using IdApp.Services.Storage;
using System;
using System.ComponentModel;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Inventory;

namespace IdApp.Services.Tag
{
	/// <summary>
	/// The TAG Profile is the heart of the digital identity for a specific user/device.
	/// Use this instance to add and make a profile more complete.
	/// The TAG Profile holds relevant data connected to not only where the user is in the registration process,
	/// but also Xmpp identifiers.
	/// </summary>
	[DefaultImplementation(typeof(TagProfile))]
	public interface ITagProfile
	{
		/// <summary>
		/// An event that triggers during the registration/profile build process, as the profile becomes more/less complete.
		/// </summary>
		event EventHandler StepChanged;

		/// <summary>
		/// An event that fires whenever any property on the <see cref="ITagProfile"/> changes.
		/// </summary>
		event PropertyChangedEventHandler Changed;

		/// <summary>
		/// The domain this profile is connected to.
		/// </summary>
		string Domain { get; }

		/// <summary>
		/// API Key, for creating new account.
		/// </summary>
		string ApiKey { get; }

		/// <summary>
		/// API Secret, for creating new account.
		/// </summary>
		string ApiSecret { get; }
		
		/// <summary>
		/// Verified phone number.
		/// </summary>
		string PhoneNumber { get; }

		/// <summary>
		/// If connecting to the domain can be done using default parameters (host=domain, default c2s port).
		/// </summary>
		bool DefaultXmppConnectivity { get; }

		/// <summary>
		/// The account name for this profile
		/// </summary>
		string Account { get; }

		/// <summary>
		/// A hash of the current password.
		/// </summary>
		string PasswordHash { get; }

		/// <summary>
		/// The hash method used for hashing the password.
		/// </summary>
		string PasswordHashMethod { get; }

		/// <summary>
		/// The Jabber Legal JID for this user/profile.
		/// </summary>
		string LegalJid { get; }

		/// <summary>
		/// The Thing Registry JID
		/// </summary>
		string RegistryJid { get; }

		/// <summary>
		/// The XMPP server's provisioning Jid.
		/// </summary>
		string ProvisioningJid { get; }

		/// <summary>
		/// The XMPP server's file upload Jid.
		/// </summary>
		string HttpFileUploadJid { get; }

		/// <summary>
		/// The XMPP server's max size for file uploads.
		/// </summary>
		long? HttpFileUploadMaxSize { get; }

		/// <summary>
		/// The XMPP server's log Jid.
		/// </summary>
		string LogJid { get; }

		/// <summary>
		/// The XMPP server's multi-user chat Jid.
		/// </summary>
		string MucJid { get; }

		/// <summary>
		/// The XMPP server's eDaler service JID.
		/// </summary>
		string EDalerJid { get; }

		/// <summary>
		/// This profile's current registration step.
		/// </summary>
		RegistrationStep Step { get; }

		/// <summary>
		/// Returns <c>true</c> if the PIN is valid, <c>false</c> otherwise.
		/// </summary>
		bool PinIsValid { get; }

		/// <summary>
		/// Returns <c>true</c> if file upload is supported for the specified XMPP server, <c>false</c> otherwise.
		/// </summary>
		bool FileUploadIsSupported { get; }

		/// <summary>
		/// The user's PIN value.
		/// </summary>
		string Pin { set; }

		/// <summary>
		/// A hashed version of the user's <see cref="Pin"/>.
		/// </summary>
		string PinHash { get; }

		/// <summary>
		/// Returns <c>true</c> if the <see cref="Pin"/> should be used, <c>false</c> otherwise.
		/// </summary>
		bool UsePin { get; }

		/// <summary>
		/// The legal identity of the curren user/profile.
		/// </summary>
		LegalIdentity LegalIdentity { get; }

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> has changed values and need saving, <c>false</c> otherwise.
		/// </summary>
		bool IsDirty { get; }

		/// <summary>
		/// Converts the current <see cref="ITagProfile"/> to a <see cref="TagConfiguration"/> object that can be persisted to the <see cref="IStorageService"/>.
		/// </summary>
		/// <returns>Configuration object</returns>
		TagConfiguration ToConfiguration();

		/// <summary>
		/// Copies values from the <see cref="TagConfiguration"/> to this instance.
		/// </summary>
		/// <param name="configuration"></param>
		void FromConfiguration(TagConfiguration configuration);

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its values updated, <c>false</c> otherwise.
		/// </summary>
		/// <returns>If values need updating</returns>
		bool NeedsUpdating();

		/// <summary>
		/// Returns <c>true</c> if the current <see cref="ITagProfile"/> needs to have its legal identity updated, <c>false</c> otherwise.
		/// </summary>
		/// <returns>If legal identity need updating</returns>
		bool LegalIdentityNeedsUpdating();

		/// <summary>
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete or is just awaiting validation, <c>false</c> otherwise.</returns>
		bool IsCompleteOrWaitingForValidation();

		/// <summary>
		/// Returns <c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.
		/// </summary>
		/// <returns><c>true</c> if the registration process for this <see cref="ITagProfile"/> is either fully complete, <c>false</c> otherwise.</returns>
		bool IsComplete();

		/// <summary>
		/// Resets the <see cref="IsDirty"/> flag, can be used after persisting values to <see cref="IStorageService"/>.
		/// </summary>
		void ResetIsDirty();

		/// <summary>
		/// Step 1 - set the domain name to connect to.
		/// </summary>
		/// <param name="PhoneNumber">Verified phone number.</param>
		void SetPhone(string PhoneNumber);

		/// <summary>
		/// Step 1 - set the domain name to connect to.
		/// </summary>
		/// <param name="domainName">The domain name.</param>
		/// <param name="defaultXmppConnectivity">If connecting to the domain can be done using default parameters (host=domain, default c2s port).</param>
		/// <param name="Key">Key to use, if an account is to be created.</param>
		/// <param name="Secret">Secret to use, if an account is to be created.</param>
		void SetDomain(string domainName, bool defaultXmppConnectivity, string Key, string Secret);

		/// <summary>
		/// Revert Step 1.
		/// </summary>
		void ClearDomain();

		/// <summary>
		/// Step 2 - set the account name and password for a <em>new</em> account.
		/// </summary>
		/// <param name="accountName">The account/user name.</param>
		/// <param name="clientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="clientPasswordHashMethod">The hash method used when hashing the password.</param>
		void SetAccount(string accountName, string clientPasswordHash, string clientPasswordHashMethod);

		/// <summary>
		/// Step 2 and 3 - set the account name and password for an <em>existing</em> account.
		/// </summary>
		/// <param name="accountName">The account/user name.</param>
		/// <param name="clientPasswordHash">The password hash (never send the real password).</param>
		/// <param name="clientPasswordHashMethod">The hash method used when hashing the password.</param>
		/// <param name="identity">The new identity.</param>
		void SetAccountAndLegalIdentity(string accountName, string clientPasswordHash, string clientPasswordHashMethod, LegalIdentity identity);

		/// <summary>
		/// Revert Step 2.
		/// </summary>
		void ClearAccount();

		/// <summary>
		/// Step 3 - set the legal identity of a newly created account.
		/// </summary>
		/// <param name="legalIdentity">The legal identity to use.</param>
		void SetLegalIdentity(LegalIdentity legalIdentity);

		/// <summary>
		/// Revert Step 3.
		/// </summary>
		void ClearLegalIdentity();

		/// <summary>
		/// Step 4 - set the current legal identity as validated.
		/// </summary>
		void SetIsValidated();

		/// <summary>
		/// Revert Step 4.
		/// </summary>
		void ClearIsValidated();

		/// <summary>
		///  Step 5 - Set a pin to use for protecting the account.
		/// </summary>
		/// <param name="pin">The pin to use.</param>
		/// <param name="shouldUsePin"><c>true</c> to use the pin, <c>false</c> otherwise.</param>
		void SetPin(string pin, bool shouldUsePin);

		/// <summary>
		/// Revert Step 5.
		/// </summary>
		void ClearPin();

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the revoked identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="revokedIdentity">The revoked identity to use.</param>
		void RevokeLegalIdentity(LegalIdentity revokedIdentity);

		/// <summary>
		/// Sets the current <see cref="LegalIdentity"/> to the compromised identity, and reverses the <see cref="Step"/> property.
		/// </summary>
		/// <param name="compromisedIdentity">The compromised identity to use.</param>
		void CompromiseLegalIdentity(LegalIdentity compromisedIdentity);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the legal id.
		/// </summary>
		/// <param name="legalJid">The legal id.</param>
		void SetLegalJid(string legalJid);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the provisioning id.
		/// </summary>
		/// <param name="provisioningJid"></param>
		void SetProvisioningJid(string provisioningJid);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the registry id.
		/// </summary>
		/// <param name="registryJid"></param>
		void SetRegistryJid(string registryJid);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the file upload parameters.
		/// </summary>
		/// <param name="httpFileUploadJid">The http file upload id.</param>
		/// <param name="maxSize">The max size allowed.</param>
		void SetFileUploadParameters(string httpFileUploadJid, long? maxSize);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the log id.
		/// </summary>
		/// <param name="logJid">The log id.</param>
		void SetLogJid(string logJid);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the multi-user chat id.
		/// </summary>
		/// <param name="mucJid">The multi-user chat id.</param>
		void SetMucJid(string mucJid);

		/// <summary>
		/// Used during Xmpp service discovery. Sets the eDaler service JID.
		/// </summary>
		/// <param name="eDalerJid">The eDaler service JID.</param>
		void SetEDalerJid(string eDalerJid);

		/// <summary>
		/// Computes a hash of the specified PIN.
		/// </summary>
		/// <param name="pin">The PIN whose hash to compute.</param>
		/// <returns>Hash Digest</returns>
		string ComputePinHash(string pin);

		/// <summary>
		/// Clears the entire profile.
		/// </summary>
		void ClearAll();
	}
}