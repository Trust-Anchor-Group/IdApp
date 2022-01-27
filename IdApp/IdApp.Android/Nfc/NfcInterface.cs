using Android.Nfc;
using Android.Nfc.Tech;
using IdApp.Nfc;
using System;
using System.Threading.Tasks;

namespace IdApp.Android.Nfc
{
	/// <summary>
	/// Abstract base class for NFC Interfaces.
	/// </summary>
	public abstract class NfcInterface : INfcInterface
	{
		/// <summary>
		/// Underlying Android Tag object.
		/// </summary>
		protected readonly Tag tag;

		/// <summary>
		/// Underlying Android Technology object.
		/// </summary>
		protected readonly BasicTagTechnology technology;

		/// <summary>
		/// Abstract base class for NFC Interfaces.
		/// </summary>
		/// <param name="Tag">Underlying Android Tag object.</param>
		/// <param name="Technology">Underlying Android Technology object.</param>
		public NfcInterface(Tag Tag, BasicTagTechnology Technology)
		{
			this.tag = Tag;
			this.technology = Technology;
		}

		/// <summary>
		/// NFC Tag
		/// </summary>
		public INfcTag Tag
		{
			get;
			internal set;
		}

		/// <summary>
		/// Connects the interface, if not connected.
		/// </summary>
		/// <returns></returns>
		public async Task OpenIfClosed()
		{
			if (!this.technology.IsConnected)
				await this.technology.ConnectAsync();
		}

		/// <summary>
		/// Closes the interface
		/// </summary>
		public void Close()
		{
			if (this.technology.IsConnected)
				this.technology.Close();
		}

		/// <summary>
		/// <see cref="IDisposable.Dispose"/>
		/// </summary>
		public void Dispose()
		{
			this.Close();
			this.technology.Dispose();
			this.tag.Dispose();
		}
	}
}