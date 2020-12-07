﻿using System;
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
        private const string TagConfigurationSettingKey = "TagConfiguration";

        private static ITagIdSdk instance;

        private FilesProvider filesProvider;

        private TagIdSdk()
        {
            this.TagProfile = new TagProfile();
            this.LogService = new LogService();
            this.AuthService = new AuthService(this.LogService);
            this.NetworkService = new NetworkService();
            this.SettingsService = new SettingsService();
            this.NeuronService = new NeuronService(this.TagProfile, this.NetworkService, this.LogService);
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
        public INetworkService NetworkService { get; }
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

            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dataFolder = Path.Combine(appDataFolder, "Data");
            if (filesProvider == null)
            {
                filesProvider = await FilesProvider.CreateAsync(dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8, (int)Constants.Timeouts.Database.TotalMilliseconds, this.AuthService.GetCustomKey);
            }
            await filesProvider.RepairIfInproperShutdown(string.Empty);
            Database.Register(filesProvider, false);

            TagConfiguration configuration = this.SettingsService.RestoreState<TagConfiguration>(TagConfigurationSettingKey);
            if (configuration == null)
            {
                configuration = new TagConfiguration();
                this.SettingsService.SaveState(TagConfigurationSettingKey, configuration);
            }
            this.TagProfile.FromConfiguration(configuration);

            await this.NeuronService.Load();
        }

        public async Task Shutdown()
        {
            await this.NeuronService.Unload();
            await Types.StopAllModules();
            filesProvider.Dispose();
            filesProvider = null;
        }

        public void AutoSave()
        {
            if (this.TagProfile.IsDirty)
            {
                this.TagProfile.ResetIsDirty();
                this.SettingsService.SaveState(TagConfigurationSettingKey, this.TagProfile.ToConfiguration());
            }
        }
    }
}