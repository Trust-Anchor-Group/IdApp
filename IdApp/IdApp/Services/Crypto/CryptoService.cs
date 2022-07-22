﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IdApp.DeviceSpecific;
using Waher.Runtime.Inventory;
using Waher.Security;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Services.Crypto
{
	[Singleton]
	internal sealed class CryptoService : ServiceReferences, ICryptoService
	{
		private readonly string basePath;
		private readonly string deviceId;
		private readonly RandomNumberGenerator rnd;

		public CryptoService()
		{
			IDeviceInformation deviceInfo = DependencyService.Get<IDeviceInformation>();

			this.basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.deviceId = deviceInfo?.GetDeviceId() + "_";
			this.rnd = RandomNumberGenerator.Create();
		}

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
	}
}
