using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tag.Sdk.Core.Services
{
    public interface IAuthService
    {
        Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);
        string CreateRandomPassword();
    }
}