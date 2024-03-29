﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Waher.Persistence;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.NewContract
{
    /// <summary>
    /// A page that allows the user to create a new contract.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class NewContractPage : IContractOptionsPage
	{
        /// <summary>
        /// Creates a new instance of the <see cref="NewContractPage"/> class.
        /// </summary>
		public NewContractPage()
        {
            this.ViewModel = new NewContractViewModel();
			this.InitializeComponent();
        }

		/// <summary>
		/// Method called (from main thread) when contract options are made available.
		/// </summary>
		/// <param name="Options">Available options, as dictionaries with contract parameters.</param>
		public async Task ShowContractOptions(IDictionary<CaseInsensitiveString, object>[] Options)
		{
			if (this.ViewModel is NewContractViewModel ViewModel)
				await ViewModel.ShowContractOptions(Options);
		}

	}
}
