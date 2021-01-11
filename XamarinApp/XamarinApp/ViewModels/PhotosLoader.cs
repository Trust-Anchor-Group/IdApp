using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private readonly ObservableCollection<ImageSource> photos;
        private readonly List<string> attachmentIds;
        private DateTime loadPhotosTimestamp;

        public PhotosLoader(ILogService logService, INetworkService networkService, INeuronService neuronService, ObservableCollection<ImageSource> photos)
        {
            this.logService = logService;
            this.networkService = networkService;
            this.neuronService = neuronService;
            this.photos = photos;
            this.attachmentIds = new List<string>();
        }

        public Task LoadPhotos(Attachment[] attachments)
        {
            return LoadPhotos(attachments, DateTime.UtcNow);
        }

        public void CancelLoadPhotos()
        {
            this.loadPhotosTimestamp = DateTime.UtcNow;
            this.photos.Clear();
            this.attachmentIds.Clear();
        }

        private async Task LoadPhotos(Attachment[] attachments, DateTime now)
        {
            if (attachments == null || attachments.Length <= 0)
                return;

            List<Attachment> attachmentsList = attachments.GetImageAttachments().ToList();
            List<string> newAttachmentIds = attachmentsList.Select(x => x.Id).ToList();

            if (this.attachmentIds.HasSameContentAs(newAttachmentIds))
                return;

            this.attachmentIds.Clear();
            this.attachmentIds.AddRange(newAttachmentIds);

            foreach (Attachment attachment in attachmentsList)
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