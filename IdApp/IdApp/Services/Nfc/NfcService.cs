﻿using IdApp.Nfc;
using IdApp.Nfc.Extensions;
using IdApp.Nfc.Records;
using IdApp.Pages;
using IdApp.Services.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Runtime.Settings;
using Waher.Security;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Services.Nfc
{
	/// <summary>
	/// Near-Field Communication (NFC) Service.
	/// </summary>
	[Singleton]
	public class NfcService : ServiceReferences, INfcService
	{
		/// <summary>
		/// Near-Field Communication (NFC) Service.
		/// </summary>
		public NfcService()
			: base()
		{
		}

		/// <summary>
		/// Method called when a new NFC Tag has been detected.
		/// </summary>
		/// <param name="Tag">NFC Tag</param>
		public async Task TagDetected(INfcTag Tag)
		{
			try
			{
				if (!await App.VerifyPin())
					return;

				string TagId = Hashes.BinaryToString(Tag.ID).ToUpper();
				NfcTagReference TagReference = await NfcTagReference.FindByTagId(TagId);

				foreach (INfcInterface Interface in Tag.Interfaces)
				{
					if (Interface is IIsoDepInterface IsoDep)
					{
						// ISO 14443-4

						string Mrz = await RuntimeSettings.GetAsync("NFC.LastMrz", string.Empty);

						if (!string.IsNullOrEmpty(Mrz) &&
							BasicAccessControl.ParseMrz(Mrz, out DocumentInformation DocInfo))
						{
							// §4.3, §D.3, https://www.icao.int/publications/Documents/9303_p11_cons_en.pdf

							byte[] Challenge = await IsoDep.GetChallenge();
							if (Challenge is not null)
							{
								byte[] ChallengeResponse = DocInfo.CalcChallengeResponse(Challenge);
								byte[] Response = await IsoDep.ExternalAuthenticate(ChallengeResponse);

								// TODO
							}
						}
					}
					else if (Interface is INdefInterface Ndef)
					{
						bool CanMakeReadOnly = await Ndef.CanMakeReadOnly();
						bool IsWritable = await Ndef.IsWritable();
						INdefRecord[] Records = await Ndef.GetMessage();

						if (Records.Length == 0)
						{
							INavigationService Nav = App.Instantiate<INavigationService>();
							if (Nav.CurrentPage is ContentBasePage ContentPage &&
								ContentPage.ViewModel is ILinkableView LinkableView &&
								LinkableView.IsLinkable &&
								IsWritable)
							{
								string Link = LinkableView.Link;
								string Title = await LinkableView.Title;

								List<object> Items = new();

								if (LinkableView.EncodeAppLinks)
									Items.Add(Title);

								Items.Add(new Uri(Link));

								if (LinkableView.EncodeAppLinks)
								{
									Items.Add(new Uri(Constants.References.AndroidApp));
									Items.Add(new Uri(Constants.References.IPhoneApp));
								}

								if (LinkableView.HasMedia)
									Items.Add(new KeyValuePair<byte[], string>(LinkableView.Media, LinkableView.MediaContentType));

								bool Ok = await Ndef.SetMessage(Items.ToArray());

								if (!Ok && LinkableView.HasMedia)
								{
									Items.RemoveAt(Items.Count - 1);
									Ok = await Ndef.SetMessage(Items.ToArray());
								}

								if (!Ok)
								{
									while (Items.Count > 2)
										Items.RemoveAt(2);

									Ok = await Ndef.SetMessage(Items.ToArray());

									if (!Ok)
									{
										Items.RemoveAt(0);
										Ok = await Ndef.SetMessage(Items.ToArray());
									}
								}

								if (Ok)
								{
									await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"],
										string.Format(LocalizationResourceManager.Current["TagEngraved"], Title));
								}
								else
								{
									await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"],
										string.Format(LocalizationResourceManager.Current["TagNotEngraved"], Title));
								}

								// TODO: Make read-only if able

								return;
							}
						}
						else
						{
							foreach (INdefRecord Record in Records)
							{
								if (Record is INdefUriRecord UriRecord)
								{
									if (!string.IsNullOrEmpty(Constants.UriSchemes.GetScheme(UriRecord.Uri)))
									{
										if (await App.OpenUrlAsync(UriRecord.Uri))
											return;
									}
								}
							}

							// TODO: Open NFC view
						}
					}
					//else if (Interface is INfcAInterface NfcA)
					//{
					//	byte[] Atqa = await NfcA.GetAtqa();
					//	short Sqk = await NfcA.GetSqk();
					//
					//	// TODO
					//}
					//else if (Interface is INfcBInterface NfcB)
					//{
					//	byte[] ApplicationData = await NfcB.GetApplicationData();
					//	byte[] ProtocolInfo = await NfcB.GetProtocolInfo();
					//
					//	// TODO
					//}
					//else if (Interface is INfcFInterface NfcF)
					//{
					//	byte[] Manufacturer = await NfcF.GetManufacturer();
					//	byte[] SystemCode = await NfcF.GetSystemCode();
					//
					//	// TODO
					//}
					//else if (Interface is INfcVInterface NfcV)
					//{
					//	sbyte DsfId = await NfcV.GetDsfId();
					//	short ResponseFlags = await NfcV.GetResponseFlags();
					//
					//	// TODO
					//}
					//else if (Interface is INfcBarcodeInterface Barcode)
					//{
					//	byte[] Data = await Barcode.ReadAllData();
					//
					//	// TODO
					//}
					//else if (Interface is INdefFormatableInterface NdefFormatable)
					//{
					//	// TODO
					//}
					//else if (Interface is IMifareUltralightInterface MifareUltralight)
					//{
					//	byte[] Data = await MifareUltralight.ReadAllData();
					//
					//	// TODO
					//}
					//else if (Interface is IMifareClassicInterface MifareClassic)
					//{
					//	byte[] Data = await MifareClassic.ReadAllData();
					//
					//	// TODO
					//}
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}
	}
}
