using System.Collections.Generic;
using System.Threading.Tasks;

namespace XamarinApp.Services
{
    public interface IAuthService
    {
        Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);
        string CreateRandomPassword();
    }
}