﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Tag.Sdk.Core;
using Tag.Sdk.Core.Extensions;
using Tag.Sdk.Core.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using Xamarin.Forms;

namespace XamarinApp.ViewModels
{
    public class PhotosLoader
    {
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private readonly INeuronService neuronService;
        private DateTime loadPhotosTimestamp;

        public PhotosLoader(ILogService logService, INetworkService networkService, INeuronService neuronService)
        {
            this.logService = logService;
            this.networkService = networkService;
            this.neuronService = neuronService;
        }

        public Task LoadPhotos(Attachment[] attachments, ObservableCollection<ImageSource> photos)
        {
            return LoadPhotos(attachments, photos, DateTime.UtcNow);
        }

        public void CancelLoadPhotos()
        {
            this.loadPhotosTimestamp = DateTime.UtcNow;
        }

        private async Task LoadPhotos(Attachment[] attachments, ObservableCollection<ImageSource> photos, DateTime now)
        {
            if (attachments == null || attachments.Length <= 0 || photos == null)
                return;

            foreach (Attachment attachment in attachments.GetImageAttachments())
            {
                if (this.loadPhotosTimestamp > now)
                {
                    return;
                }

                try
                {
                    KeyValuePair<string, TemporaryFile> pair;

                    if (!this.networkService.IsOnline || !this.neuronService.Contracts.IsOnline)
                        continue;
                    
                    pair = await this.neuronService.Contracts.GetContractAttachmentAsync(attachment.Url, Constants.Timeouts.DownloadFile);

                    if (this.loadPhotosTimestamp > now)
                    {
                        return;
                    }

                    using (TemporaryFile file = pair.Value)
                    {
                        file.Reset();
                        MemoryStream ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        ms.Reset();
                        ImageSource imageSource = ImageSource.FromStream(() => ms);
                        Device.BeginInvokeOnMainThread(() => photos.Add(imageSource));
                    }
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex);
                }
            }
        }
    }
}