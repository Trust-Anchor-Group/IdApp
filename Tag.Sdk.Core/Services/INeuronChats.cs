﻿using System;

namespace Tag.Sdk.Core.Services
{
    /// <summary>
    /// Adds support for chats.
    /// </summary>
    public interface INeuronChats : IDisposable
    {
        bool IsOnline { get; }
    }
}