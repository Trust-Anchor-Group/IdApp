using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tag.Neuron.Xamarin.Services
{
    public interface IAuthService
    {
        Task<KeyValuePair<byte[], byte[]>> GetCustomKey(string fileName);
        string CreateRandomPassword();
    }
}