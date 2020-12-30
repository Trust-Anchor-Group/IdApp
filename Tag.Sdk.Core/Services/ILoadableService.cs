﻿using System;
using System.Threading.Tasks;

namespace Tag.Sdk.Core.Services
{
    public interface ILoadableService
    {
        Task Load(bool isResuming);
        Task Unload();
        event EventHandler<LoadedEventArgs> Loaded;
    }
}