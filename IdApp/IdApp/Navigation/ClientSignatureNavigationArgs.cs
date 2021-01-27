﻿using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    public class ClientSignatureNavigationArgs : NavigationArgs
    {
        public ClientSignatureNavigationArgs(ClientSignature signature, LegalIdentity identity)
        {
            this.Signature = signature;
            this.Identity = identity;
        }

        public ClientSignature Signature { get; }
        public LegalIdentity Identity { get; }
    }
}