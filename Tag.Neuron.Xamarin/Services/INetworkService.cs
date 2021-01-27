﻿using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Tag.Neuron.Xamarin.Services
{
    /// <summary>
    /// The network service is a wafer-thin wrapper around the <see cref="Xamarin.Essentials.Connectivity"/> object.
    /// It exposes an event handler for monitoring connected state, and a DNS lookup method.
    /// It also has helper methods to make network requests and catch and display errors if they fail.
    /// </summary>
    public interface INetworkService : IDisposable
    {
        /// <summary>
        /// Triggers whenever network connectivity chanes.
        /// </summary>
        event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
        /// <summary>
        /// Performs a DNS lookup for the specified domain name.
        /// </summary>
        /// <param name="domainName">The domain name whose name to resolve.</param>
        /// <returns></returns>
        Task<(string hostName, int port)> GetXmppHostnameAndPort(string domainName);
        /// <summary>
        /// Determines whether we have network (wifi/cellular/other) or not.
        /// </summary>
        bool IsOnline { get; }

        Task<bool> TryRequest(Func<Task> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1>(Func<TIn1, Task> func, TIn1 p1, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2>(Func<TIn1, TIn2, Task> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3>(Func<TIn1, TIn2, TIn3, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4>(Func<TIn1, TIn2, TIn3, TIn4, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<bool> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");

        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TReturn>( Func<Task<TReturn>> func, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn, TReturn>(Func<TIn, Task<TReturn>> func, TIn p1, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TReturn>(Func<TIn1, TIn2, Task<TReturn>> func, TIn1 p1, TIn2 p2, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TReturn>(Func<TIn1, TIn2, TIn3, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
        Task<(bool Succeeded, TReturn ReturnValue)> TryRequest<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, TReturn>(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TIn9, TIn10, TIn11, Task<TReturn>> func, TIn1 p1, TIn2 p2, TIn3 p3, TIn4 p4, TIn5 p5, TIn6 p6, TIn7 p7, TIn8 p8, TIn9 p9, TIn10 p10, TIn11 p11, bool rethrowException = false, bool displayAlert = true, [CallerMemberName] string memberName = "");
    }
}