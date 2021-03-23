using System;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Extensions;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Tests.Extensions
{
    public class LegalIdentityExtensionsTests
    {
        [Test]
        public void NeedsUpdating_ReturnsTrue_IfIdentityIsNull()
        {
            Assert.IsTrue(((LegalIdentity)null).NeedsUpdating());
        }

        [Test]
        public void IsCreatedOrApproved_ReturnsFalse_IfIdentityIsNull()
        {
            Assert.IsFalse(((LegalIdentity)null).IsCreatedOrApproved());
        }

        [Test]
        [TestCase(IdentityState.Created, true)]
        [TestCase(IdentityState.Approved, true)]
        [TestCase(IdentityState.Compromised, false)]
        [TestCase(IdentityState.Obsoleted, false)]
        [TestCase(IdentityState.Rejected, false)]
        public void IsCreatedOrApproved(IdentityState state, bool expected)
        {
            Assert.AreEqual(expected, new LegalIdentity { State = state }.IsCreatedOrApproved());
        }

        [Test]
        [TestCase(IdentityState.Created, false)]
        [TestCase(IdentityState.Approved, false)]
        [TestCase(IdentityState.Compromised, true)]
        [TestCase(IdentityState.Obsoleted, true)]
        [TestCase(IdentityState.Rejected, true)]
        public void NeedsUpdating(IdentityState state, bool expected)
        {
            Assert.AreEqual(expected, new LegalIdentity { State = state }.NeedsUpdating());
        }

        private const string DefaultValue = "*";

        [Test]
        public void GetJid_ReturnsDefaultValue_IfIdentityIsNull()
        {
            Assert.AreEqual(DefaultValue, ((LegalIdentity)null).GetJid(DefaultValue));
        }

        [Test]
        public void GetJid_ReturnsDefaultValue_IfPropertiesAreMissing()
        {
            LegalIdentity identity = new LegalIdentity
            {
                Id = Guid.NewGuid().ToString(), 
                State = IdentityState.Approved
            };

            Assert.AreEqual(DefaultValue, identity.GetJid(DefaultValue));
        }

        [Test]
        public void GetJid_ReturnsDefaultValue_IfNotFound()
        {
            LegalIdentity identity = new LegalIdentity
            {
                Id = Guid.NewGuid().ToString(), 
                State = IdentityState.Approved,
                Properties = new []
                {
                    new Property("Foo", "Bar")
                }
            };

            Assert.AreEqual(DefaultValue, identity.GetJid(DefaultValue));
        }

        [Test]
        public void GetJid()
        {
            LegalIdentity identity = new LegalIdentity
            {
                Id = Guid.NewGuid().ToString(), 
                State = IdentityState.Approved,
                Properties = new []
                {
                    new Property(Constants.XmppProperties.Jid, "42")
                }
            };

            Assert.AreEqual("42", identity.GetJid(DefaultValue));
        }
    }
}