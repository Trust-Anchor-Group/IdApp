﻿namespace Tag.Sdk.Core
{
    /// <summary>
    /// Dependency interface for device-specific information.
    /// </summary>
    public interface IDeviceInformation
    {
        /// <summary>
        /// Gets the ID of the device.
        /// </summary>
        string GetDeviceID();
    }
}