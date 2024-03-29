﻿using System;
using System.Threading.Tasks;
using IdApp.Services.Navigation;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms.Xaml;

namespace IdApp.Pages.Contracts.ViewContract
{
	/// <summary>
	/// A page that displays a specific contract.
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ViewContractPage"/> class.
		/// </summary>
		public ViewContractPage()
		{
			this.ViewModel = new ViewContractViewModel();
			this.InitializeComponent();
		}

		/// <inheritdoc/>
		protected override Task OnDisappearingAsync()
		{
			this.PhotoViewer.HidePhotos();
			return base.OnDisappearingAsync();
		}

		private void Image_Tapped(object Sender, EventArgs e)
		{
			if (this.ViewModel is ViewContractViewModel ViewContractViewModel)
			{
				Attachment[] attachments = ViewContractViewModel.Contract?.Attachments;
				this.PhotoViewer.ShowPhotos(attachments);
			}
		}
	}
}
