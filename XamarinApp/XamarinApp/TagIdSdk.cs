using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Script;
using XamarinApp.Services;

namespace XamarinApp
{
    public class TagIdSdk : ITagIdSdk
    {
        private static ITagIdSdk instance;

        private FilesProvider filesProvider;

        private TagIdSdk()
        {
            
        }

        public void Dispose()
        {
            if (instance != null)
            {
                Types.StopAllModules().GetAwaiter().GetResult();
                instance = null;
            }
        }

        public static ITagIdSdk Instance
        {
            get => instance ?? (instance = new TagIdSdk());
            protected internal set => instance = value;
        }

        public async Task Startup(IAuthService authService)
        {
            Types.Initialize(
                typeof(App).Assembly,
                typeof(Database).Assembly,
                typeof(FilesProvider).Assembly,
                typeof(ObjectSerializer).Assembly,
                typeof(XmppClient).Assembly,
                typeof(ContractsClient).Assembly,
                typeof(Expression).Assembly,
                typeof(XmppServerlessMessaging).Assembly);

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, authService.GetCustomKey);
            }
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            Database.Register(filesProvider, false);
        }

        public async Task Shutdown()
        {
            await DatabaseModule.Flush();
            filesProvider.Dispose();
            filesProvider = null;
        }
    }
}