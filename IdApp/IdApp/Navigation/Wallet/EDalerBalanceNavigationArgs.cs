﻿using Tag.Neuron.Xamarin.Services;
using EDaler;

namespace IdApp.Navigation.Wallet
{
    /// <summary>
    /// Holds navigation parameters specific to an eDaler balance event.
    /// </summary>
    public class EDalerBalanceNavigationArgs : NavigationArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="EDalerBalanceNavigationArgs"/> class.
        /// </summary>
        /// <param name="Balance">Balance information.</param>
        public EDalerBalanceNavigationArgs(Balance Balance)
        {
            this.Balance = Balance;
        }
        
        /// <summary>
        /// eDaler balance object.
        /// </summary>
        public Balance Balance { get; }
    }
}