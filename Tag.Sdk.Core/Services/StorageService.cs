using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;

namespace Tag.Sdk.Core.Services
{
    internal sealed class StorageService : IStorageService
    {
        private readonly ILogService logService;

        public StorageService(ILogService logService)
        {
            this.logService = logService;
        }

        public Task Insert(object obj)
        {
            try
            {
                return Database.Insert(obj);
            }
            catch (Exception e)
            {
                this.logService.LogException(e, new KeyValuePair<string, string>("Class", nameof(StorageService)), new KeyValuePair<string, string>("Method", nameof(Insert)));
                throw;
            }
        }

        public Task Update(object obj)
        {
            try
            {
                return Database.Update(obj);
            }
            catch (Exception e)
            {
                this.logService.LogException(e, new KeyValuePair<string, string>("Class", nameof(StorageService)), new KeyValuePair<string, string>("Method", nameof(Update)));
                throw;
            }
        }

        public Task<T> FindFirstDeleteRest<T>() where T : class
        {
            try
            {
                return Database.FindFirstDeleteRest<T>();
            }
            catch (Exception e)
            {
                this.logService.LogException(e, new KeyValuePair<string, string>("Class", nameof(StorageService)), new KeyValuePair<string, string>("Method", nameof(FindFirstDeleteRest)));
                throw;
            }
        }

        public Task<T> FindFirstIgnoreRest<T>() where T : class
        {
            try
            {
                return Database.FindFirstIgnoreRest<T>();
            }
            catch (Exception e)
            {
                this.logService.LogException(e, new KeyValuePair<string, string>("Class", nameof(StorageService)), new KeyValuePair<string, string>("Method", nameof(FindFirstIgnoreRest)));
                throw;
            }
        }
    }
}