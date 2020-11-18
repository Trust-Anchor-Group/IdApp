using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.LifeCycle;

namespace XamarinApp.Services
{
    internal sealed class StorageService : LoadableService, IStorageService
    {
        private readonly IAuthService authService;
        private FilesProvider filesProvider;

        public StorageService(IAuthService authService)
        {
            this.authService = authService;
        }

        public override async Task Load()
        {
            BeginLoad();
            try
            {
                string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string DataFolder = Path.Combine(AppDataFolder, "Data");
                if (filesProvider == null)
                {
                    filesProvider = await FilesProvider.CreateAsync(DataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, 10000, this.authService.GetCustomKey);
                }
                await filesProvider.RepairIfInproperShutdown(string.Empty);
                Database.Register(filesProvider, false);
                EndLoad(true);
            }
            catch (Exception)
            {
                EndLoad(false);
            }
        }

        public override async Task Unload()
        {
            BeginUnload();
            await DatabaseModule.Flush();
            this.filesProvider.Dispose();
            this.filesProvider = null;
            EndUnload();
        }

        public Task Insert(object obj)
        {
            return Database.Insert(obj);
        }

        public Task Update(object obj)
        {
            return Database.Insert(obj);
        }

        public Task<T> FindFirstDeleteRest<T>() where T : class
        {
            return Database.FindFirstDeleteRest<T>();
        }

        public Task<T> FindFirstIgnoreRest<T>() where T : class
        {
            return Database.FindFirstIgnoreRest<T>();
        }
    }
}