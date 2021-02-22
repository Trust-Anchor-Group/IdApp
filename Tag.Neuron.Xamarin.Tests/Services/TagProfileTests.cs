using System;
using NUnit.Framework;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace Tag.Neuron.Xamarin.Tests.Services
{
    public class TagProfileTests
    {
        private static LegalIdentity CreateIdentity(IdentityState state)
        {
            return new LegalIdentity
            {
                State = state,
                Provider = "provider",
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                From = DateTime.UtcNow,
                To = DateTime.MaxValue,
                ClientKeyName = "key"
            };
        }

        [Test]
        public void SetDomain_ToAValidValue_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
        }

        [Test]
        public void SetDomain_ToAnInvalidValue_DoesNotIncrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("");
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
        }

        [Test]
        public void ClearDomain_DecrementsStepToOperator()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.ClearDomain();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
        }

        [Test]
        public void SetAccount_ToAValidValue_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void SetAccount_ToAValidValue_WhenNotOnAccountStep_DoesNotIncrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
        }

        [Test]
        public void SetAccount_ToAnInvalidValue_DoesNotIncrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
        }

        [Test]
        public void ClearAccount_DecrementsStepToOperator()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            tagProfile.ClearAccount();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
        }

        [Test]
        public void SetAccountAndLegalIdentity_ToAValidValue_WhereIdentityIsCreated_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Created };
            tagProfile.SetAccountAndLegalIdentity("account", "hash", "hashMethod", identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
        }

        [Test]
        public void SetAccountAndLegalIdentity_ToAValidValue_WhereIdentityIsApproved_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetAccountAndLegalIdentity("account", "hash", "hashMethod", identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
        }

        [Test]
        public void SetAccountAndLegalIdentity_ToAValidValue_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Compromised };
            tagProfile.SetAccountAndLegalIdentity("account", "hash", "hashMethod", identity);
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void SetAccountAndLegalIdentity_ToAnInvalidValue1_DoesNotIncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccountAndLegalIdentity("account", "hash", "hashMethod", null);
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
        }

        [Test]
        public void SetAccountAndLegalIdentity_ToAnInvalidValue2_DoesNotIncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Compromised };
            tagProfile.SetAccountAndLegalIdentity("", "hash", "hashMethod", identity);
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
        }

        [Test]
        public void SetLegalIdentity_ToAValidValue1_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Created };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
        }

        [Test]
        public void SetLegalIdentity_ToAValidValue2_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
        }

        [Test]
        public void SetLegalIdentity_ToAnInvalidValue_DoesNotIncrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);

            LegalIdentity identity = new LegalIdentity { State = IdentityState.Compromised };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            identity.State = IdentityState.Obsoleted;
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            identity.State = IdentityState.Rejected;
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void ClearLegalIdentity_DecrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            tagProfile.ClearLegalIdentity();
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
        }

        [Test]
        public void RevokeLegalIdentity_DecrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = CreateIdentity(IdentityState.Created);
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.RevokeLegalIdentity(CreateIdentity(IdentityState.Compromised));
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void CompromiseLegalIdentity_DecrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = CreateIdentity(IdentityState.Created);
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.CompromiseLegalIdentity(CreateIdentity(IdentityState.Compromised));
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void SetIsValidated_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.SetIsValidated();
            Assert.AreEqual(RegistrationStep.Pin, tagProfile.Step);
        }

        [Test]
        public void SetIsValidated_WhenNotValidated_DoesNotIncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            tagProfile.SetIsValidated();
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void ClearIsValidated_DecrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.ClearIsValidated();
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            Assert.IsNull(tagProfile.LegalIdentity);
            Assert.IsNull(tagProfile.LegalJid);
        }

        [Test]
        public void SetPin_IncrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.SetIsValidated();
            Assert.AreEqual(RegistrationStep.Pin, tagProfile.Step);
            tagProfile.SetPin("pin", false);
            Assert.AreEqual(RegistrationStep.Complete, tagProfile.Step);
        }

        [Test]
        public void SetPin_IfNotOnPinStep_DoesNotIncrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            tagProfile.SetPin("pin", false);
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
        }

        [Test]
        public void ClearPin_WhenNotComplete_DecrementsStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.SetIsValidated();
            Assert.AreEqual(RegistrationStep.Pin, tagProfile.Step);
            tagProfile.ClearPin();
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
        }

        [Test]
        public void ClearPin_WhenComplete_DoesNotDecrementStep()
        {
            TagProfile tagProfile = new TagProfile();
            Assert.AreEqual(RegistrationStep.Operator, tagProfile.Step);
            tagProfile.SetDomain("domain");
            Assert.AreEqual(RegistrationStep.Account, tagProfile.Step);
            tagProfile.SetAccount("account", "hash", "hashMethod");
            Assert.AreEqual(RegistrationStep.RegisterIdentity, tagProfile.Step);
            LegalIdentity identity = new LegalIdentity { State = IdentityState.Approved };
            tagProfile.SetLegalIdentity(identity);
            Assert.AreEqual(RegistrationStep.ValidateIdentity, tagProfile.Step);
            tagProfile.SetIsValidated();
            Assert.AreEqual(RegistrationStep.Pin, tagProfile.Step);
            tagProfile.SetPin("pin", false);
            Assert.AreEqual(RegistrationStep.Complete, tagProfile.Step);
            tagProfile.ClearPin();
            Assert.AreEqual(RegistrationStep.Complete, tagProfile.Step);
        }
    }
}