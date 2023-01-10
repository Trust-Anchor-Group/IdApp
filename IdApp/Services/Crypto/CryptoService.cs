using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdApp.DeviceSpecific;
using Waher.Runtime.Inventory;
using Waher.Security;
using Waher.Security.JWT;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Services.Crypto
{
	/// <summary>
	/// Cryptographic service that helps create passwords and other security related tasks.
	/// </summary>
	[Singleton]
	internal sealed class CryptoService : ServiceReferences, ICryptoService
	{
		private readonly string basePath;
		private readonly string deviceId;
		private readonly RandomNumberGenerator rnd;
		private JwtFactory jwtFactory;

		/// <summary>
		/// Device ID
		/// </summary>
		public string DeviceID => this.deviceId;

		/// <summary>
		/// Cryptographic service that helps create passwords and other security related tasks.
		/// </summary>
		public CryptoService()
		{
			IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();

			this.basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.deviceId = deviceInfo?.GetDeviceId() + "_";
			this.rnd = RandomNumberGenerator.Create();
		}

		/// <summary>
		/// Returns a cryptographic authorization key for the given filename.
		/// </summary>
		/// <param name="fileName">The filename to get a key for.</param>
		/// <returns>A cryptographic key.</returns>
		public async Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName)
		{
			byte[] key;
			byte[] iv;
			string s;
			int i;

			string FileNameHash = this.deviceId + Path.GetRelativePath(this.basePath, fileName);

			try
			{
				s = await SecureStorage.GetAsync(FileNameHash);
			}
			catch (TypeInitializationException ex)
			{
				this.LogService.LogException(ex);
				// No secure storage available.

				key = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".Key"));
				iv = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".IV"));
				Array.Resize<byte>(ref iv, 16);

				return new KeyValuePair<byte[], byte[]>(key, iv);
			}

			if (!string.IsNullOrWhiteSpace(s) && (i = s.IndexOf(',')) > 0)
			{
				key = Hashes.StringToBinary(s.Substring(0, i));
				iv = Hashes.StringToBinary(s[(i + 1)..]);
			}
			else
			{
				key = new byte[32];
				iv = new byte[16];

				lock (this.rnd)
				{
					this.rnd.GetBytes(key);
					this.rnd.GetBytes(iv);
				}

				s = Hashes.BinaryToString(key) + "," + Hashes.BinaryToString(iv);
				await SecureStorage.SetAsync(FileNameHash, s);
			}

			return new KeyValuePair<byte[], byte[]>(key, iv);
		}

		/// <summary>
		/// Generates a random password to use.
		/// </summary>
		/// <returns>Random password</returns>
		public string CreateRandomPassword()
		{
			return Hashes.BinaryToString(this.GetBytes(32));
		}

		private byte[] GetBytes(int nrBytes)
		{
			byte[] result = new byte[nrBytes];

			lock (this.rnd)
			{
				this.rnd.GetBytes(result);
			}

			return result;
		}

		/// <summary>
		/// Initializes the JWT factory.
		/// </summary>
		public async Task InitializeJwtFactory()
		{
			KeyValuePair<byte[], byte[]> Keys = await this.GetCustomKey("factory.jwt");
			this.jwtFactory = new JwtFactory(Keys.Key);
		}

		/// <summary>
		/// Generates a JWT token the app can send to third parties. The token and its claims can be parsed and
		/// validated using <see cref="ParseAndValidateJwtToken"/>.
		/// </summary>
		/// <param name="Claims">Set of claims to embed into token.</param>
		/// <returns>JWT token.</returns>
		public string GenerateJwtToken(params KeyValuePair<string,object>[] Claims)
		{
			if (this.jwtFactory is null)
				throw new Exception("JWT Factory not initialized.");

			return this.jwtFactory.Create(Claims);
		}

		/// <summary>
		/// Vaidates a JWT token, that has been issued by the same app. (Tokens from other apps will not be valid.)
		/// </summary>
		/// <param name="Token">String representation of JWT token.</param>
		/// <returns>Parsed token, if valid, null if not valid.</returns>
		public JwtToken ParseAndValidateJwtToken(string Token)
		{
			if (this.jwtFactory is null)
				return null;

			try
			{
				JwtToken Parsed = new(Token);
				if (!this.jwtFactory.IsValid(Parsed))
					return null;

				return Parsed;
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
