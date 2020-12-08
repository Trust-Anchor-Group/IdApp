﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Waher.Networking.XMPP.P2P;
using Waher.Persistence;
using Waher.Persistence.Files;
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
            this.TagProfile = new TagProfile();
            this.LogService = new LogService();
            this.AuthService = new AuthService(this.LogService);
            this.NetworkService = new NetworkService();
            this.SettingsService = new SettingsService();
            this.StorageService = new StorageService();
            this.NeuronService = new NeuronService(this.TagProfile, this.NetworkService, this.LogService);
            this.ContractsService = new ContractsService(this.TagProfile, this.NeuronService);
        }

        public void Dispose()
        {
            instance = null;
        }

        public static ITagIdSdk Create()
        {
            return instance ?? (instance = new TagIdSdk());
        }

        public TagProfile TagProfile { get; }
        public IAuthService AuthService { get; }
        public INeuronService NeuronService { get; }
        public IContractsService ContractsService { get; }
        public INetworkService NetworkService { get; }
        public IStorageService StorageService { get; }
        public ISettingsService SettingsService { get; }
        public ILogService LogService { get; }

        public async Task Startup()
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

            Stopwatch sw = Stopwatch.StartNew();
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                System.Diagnostics.Debug.WriteLine("SW1 START");
                Stopwatch sw1 = Stopwatch.StartNew();
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.AuthService.GetCustomKey);
                sw1.Stop();
                System.Diagnostics.Debug.WriteLine($"SW1 TOOK {sw1.ElapsedMilliseconds} ms");
            }
            Stopwatch sw2 = Stopwatch.StartNew();
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            sw2.Stop();
            System.Diagnostics.Debug.WriteLine($"SW2 TOOK {sw2.ElapsedMilliseconds} ms");
            sw2.Restart();
            Database.Register(filesProvider, false);
            sw2.Stop();
            System.Diagnostics.Debug.WriteLine($"SW3 TOOK {sw2.ElapsedMilliseconds} ms");

            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"SWTOTAL TOOK {sw.ElapsedMilliseconds} ms");

            TagConfiguration configuration = await this.StorageService.FindFirstDeleteRest<TagConfiguration>();
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                await this.StorageService.Insert(configuration);
            }
            this.TagProfile.FromConfiguration(configuration);

            await this.NeuronService.Load();
        }

        public async Task Shutdown()
        {
            await this.NeuronService.Unload();
            await Types.StopAllModules();
            if (this.filesProvider != null)
            {
                this.filesProvider.Dispose();
                this.filesProvider = null;
            }
        }

        public void AutoSave()
        {
            if (this.TagProfile.IsDirty)
            {
                this.TagProfile.ResetIsDirty();
                this.StorageService.Update(this.TagProfile.ToConfiguration());
            }
        }
    }
}