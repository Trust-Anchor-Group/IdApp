﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Waher.Runtime.Inventory;
using Waher.Security;
using Xamarin.Essentials;

namespace Tag.Neuron.Xamarin.Services
{
	[Singleton]
	internal sealed class CryptoService : ICryptoService
	{
		private readonly ILogService logService;
		private readonly RandomNumberGenerator rnd;

		public CryptoService(ILogService logService)
		{
			this.logService = logService;
			this.rnd = RandomNumberGenerator.Create();
		}

		public async Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName)
		{
			byte[] key;
			byte[] iv;
			string s;
			int i;

			try
			{
				s = await SecureStorage.GetAsync(fileName);
			}
			catch (TypeInitializationException ex)
			{
				this.logService.LogException(ex);
				// No secure storage available.

				key = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".Key"));
				iv = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(fileName + ".IV"));
				Array.Resize<byte>(ref iv, 16);

				return new KeyValuePair<byte[], byte[]>(key, iv);
			}

			if (!string.IsNullOrWhiteSpace(s) && (i = s.IndexOf(',')) > 0)
			{
				key = Hashes.StringToBinary(s.Substring(0, i));
				iv = Hashes.StringToBinary(s.Substring(i + 1));
			}
			else
			{
				key = new byte[32];
				iv = new byte[16];

				lock (rnd)
				{
					rnd.GetBytes(key);
					rnd.GetBytes(iv);
				}

				s = Hashes.BinaryToString(key) + "," + Hashes.BinaryToString(iv);
				await SecureStorage.SetAsync(fileName, s);
			}

			return new KeyValuePair<byte[], byte[]>(key, iv);
		}

		public string CreateRandomPassword()
		{
			return Hashes.BinaryToString(GetBytes(16));
		}

		private byte[] GetBytes(int nrBytes)
		{
			byte[] result = new byte[nrBytes];

			lock (rnd)
			{
				rnd.GetBytes(result);
			}

			return result;
		}
	}
}