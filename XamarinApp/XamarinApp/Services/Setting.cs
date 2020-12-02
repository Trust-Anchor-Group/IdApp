﻿using SQLite;

namespace XamarinApp.Services
{
    internal sealed class Setting
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }
}