﻿using System;
using System.IO;
using System.Threading.Tasks;
using IdApp.Services.Navigation;
using IdApp.Services.Neuron;
using IdApp.Services.Tag;
using Xamarin.Forms;

namespace IdApp.Pages.Identity.TransferIdentity
{
	/// <summary>
	/// The view model to bind to for when displaying identities.
	/// </summary>
	public class TransferIdentityViewModel : NeuronViewModel
	{
		private readonly INavigationService navigationService;

		/// <summary>
		/// Creates an instance of the <see cref="TransferIdentityViewModel"/> class.
		/// </summary>
		public TransferIdentityViewModel(
			ITagProfile tagProfile,
			IUiDispatcher uiDispatcher,
			INeuronService neuronService,
			INavigationService navigationService)
		: base(neuronService, uiDispatcher, tagProfile)
		{
			this.navigationService = navigationService;
		}

		/// <inheritdoc/>
		protected override async Task DoBind()
		{
			await base.DoBind();

			if (this.navigationService.TryPopArgs(out TransferIdentityNavigationArgs args))
				this.Uri = args.Uri;

			byte[] Data = IdApp.QrCode.GeneratePng(this.Uri, 400, 400);

			this.QrCode = ImageSource.FromStream(() => new MemoryStream(Data));
			this.QrCodeWidth = 400;
			this.QrCodeHeight = 400;
		}

		#region Properties

		/// <summary>
		/// See <see cref="Uri"/>
		/// </summary>
		public static readonly BindableProperty UriProperty =
			BindableProperty.Create("Uri", typeof(string), typeof(TransferIdentityViewModel), default(string));

		/// <summary>
		/// Uri date of the identity
		/// </summary>
		public string Uri
		{
			get { return (string)GetValue(UriProperty); }
			set { SetValue(UriProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeProperty"/>
		/// </summary>
		public static readonly BindableProperty QrCodeProperty =
			BindableProperty.Create("QrCode", typeof(ImageSource), typeof(TransferIdentityViewModel), default(ImageSource));

		/// <summary>
		/// Generated QR code image for the identity
		/// </summary>
		public ImageSource QrCode
		{
			get { return (ImageSource)GetValue(QrCodeProperty); }
			set { SetValue(QrCodeProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeWidth"/>
		/// </summary>
		public static readonly BindableProperty QrCodeWidthProperty =
			BindableProperty.Create("QrCodeWidth", typeof(int), typeof(TransferIdentityViewModel), UiConstants.QrCode.DefaultImageWidth);

		/// <summary>
		/// Gets or sets the width, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeWidth
		{
			get { return (int)GetValue(QrCodeWidthProperty); }
			set { SetValue(QrCodeWidthProperty, value); }
		}

		/// <summary>
		/// See <see cref="QrCodeHeight"/>
		/// </summary>
		public static readonly BindableProperty QrCodeHeightProperty =
			BindableProperty.Create("QrCodeHeight", typeof(int), typeof(TransferIdentityViewModel), UiConstants.QrCode.DefaultImageHeight);

		/// <summary>
		/// Gets or sets the height, in pixels, of the QR Code image to generate.
		/// </summary>
		public int QrCodeHeight
		{
			get { return (int)GetValue(QrCodeHeightProperty); }
			set { SetValue(QrCodeHeightProperty, value); }
		}

		#endregion

	}
}