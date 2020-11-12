using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Waher.Security;
using Xamarin.Essentials;

namespace XamarinApp.Services
{
    internal sealed class AuthService : IAuthService
    {
        private readonly RandomNumberGenerator rnd;

        public AuthService()
        {
            rnd = RandomNumberGenerator.Create();
        }

        public async Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string FileName)
        {
            byte[] Key;
            byte[] IV;
            string s;
            int i;

            try
            {
                s = await SecureStorage.GetAsync(FileName);
            }
            catch (TypeInitializationException)
            {
                // No secure storage available.

                Key = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(FileName + ".Key"));
                IV = Hashes.ComputeSHA256Hash(Encoding.UTF8.GetBytes(FileName + ".IV"));
                Array.Resize<byte>(ref IV, 16);

                return new KeyValuePair<byte[], byte[]>(Key, IV);
            }

            if (!string.IsNullOrEmpty(s) && (i = s.IndexOf(',')) > 0)
            {
                Key = Hashes.StringToBinary(s.Substring(0, i));
                IV = Hashes.StringToBinary(s.Substring(i + 1));
            }
            else
            {
                Key = new byte[32];
                IV = new byte[16];

                lock (rnd)
                {
                    rnd.GetBytes(Key);
                    rnd.GetBytes(IV);
                }

                s = Hashes.BinaryToString(Key) + "," + Hashes.BinaryToString(IV);
                await SecureStorage.SetAsync(FileName, s);
            }

            return new KeyValuePair<byte[], byte[]>(Key, IV);
        }

        public string CreateRandomPassword()
        {
            return Hashes.BinaryToString(GetBytes(16));
        }

        private byte[] GetBytes(int NrBytes)
        {
            byte[] Result = new byte[NrBytes];

            lock (rnd)
            {
                rnd.GetBytes(Result);
            }

            return Result;
        }
    }
}