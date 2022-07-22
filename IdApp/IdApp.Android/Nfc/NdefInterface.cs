using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.Android.Nfc.Records;
using IdApp.Nfc;
using IdApp.Nfc.Records;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Waher.Content;
using Waher.Runtime.Inventory;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Class handling NDEF Interfaces.
	/// </summary>
	public class NdefInterface : NfcInterface, INdefInterface
	{
		private readonly Ndef ndef;

		/// <summary>
		/// Class handling NDEF Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		public NdefInterface(Tag Tag)
			: base(Tag, Ndef.Get(Tag))
		{
			this.ndef = (Ndef)this.technology;
		}

		/// <summary>
		/// If the TAG can be made read-only
		/// </summary>
		public async Task<bool> CanMakeReadOnly()
		{
			await this.OpenIfClosed();
			return this.ndef.CanMakeReadOnly();
		}

		/// <summary>
		/// If the TAG is writable
		/// </summary>
		public async Task<bool> IsWritable()
		{
			await this.OpenIfClosed();
			return this.ndef.IsWritable;
		}

		/// <summary>
		/// Gets the message (with records) of the NDEF tag.
		/// </summary>
		public async Task<INdefRecord[]> GetMessage()
		{
			await this.OpenIfClosed();

			NdefMessage Message = this.ndef.NdefMessage;
			if (Message is null)
				return new INdefRecord[0];

			NdefRecord[] Records = Message.GetRecords();
			List<INdefRecord> Result = new();

			foreach (NdefRecord Record in Records)
			{
				switch (Record.Tnf)
				{
					case NdefRecord.TnfAbsoluteUri:
						Result.Add(new UriRecord(Record));
						break;

					case NdefRecord.TnfMimeMedia:
						Result.Add(new MimeTypeRecord(Record));
						break;

					case NdefRecord.TnfWellKnown:
						string TypeInfo = Encoding.UTF8.GetString(Record.GetTypeInfo());

						if (TypeInfo == "U")
							Result.Add(new UriRecord(Record));
						else
							Result.Add(new WellKnownTypeRecord(Record));
						break;

					case NdefRecord.TnfExternalType:
						Result.Add(new ExternalTypeRecord(Record));
						break;

					case NdefRecord.TnfEmpty:
					case NdefRecord.TnfUnchanged:
					case NdefRecord.TnfUnknown:
					default:
						break;
				}
			}

			return Result.ToArray();
		}

		/// <summary>
		/// Sets the message (with recorsd) on the NDEF tag.
		/// </summary>
		/// <param name="Items">Items to encode</param>
		/// <returns>If the items could be encoded and written to the tag.</returns>
		public async Task<bool> SetMessage(params object[] Items)
		{
			try
			{
				await this.OpenIfClosed();

				List<NdefRecord> Records = new();

				foreach (object Item in Items)
				{
					if (Item is Uri Uri)
					{
						if (Uri.IsAbsoluteUri)
							Records.Add(NdefRecord.CreateUri(Uri.AbsoluteUri));
						else
							return false;
					}
					else if (Item is string s)
						Records.Add(NdefRecord.CreateTextRecord(null, s));
					else
					{
						if (Item is not KeyValuePair<byte[], string> Mime)
						{
							if (!InternetContent.Encodes(Item, out Grade _, out IContentEncoder Encoder))
								return false;

							Mime = await Encoder.EncodeAsync(Item, Encoding.UTF8);
						}

						Records.Add(NdefRecord.CreateMime(Mime.Value, Mime.Key));
					}
				}

				NdefMessage Message = new(Records.ToArray());

				await this.ndef.WriteNdefMessageAsync(Message);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
