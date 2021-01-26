using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Extensions;
using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;
using Waher.Runtime.Temporary;
using Xamarin.Forms;
using XamarinApp.Services;

namespace XamarinApp.ViewModels
{
    public class PhotosLoader
    {
        private readonly ILogService logService;
        private readonly INetworkService networkService;
        private readonly INeuronService neuronService;
        private readonly IImageCacheService imageCacheService;
        private readonly ObservableCollection<ImageSource> photos;
        private readonly List<string> attachmentIds;
        private DateTime loadPhotosTimestamp;

        public PhotosLoader(
            ILogService logService, 
            INetworkService networkService, 
            INeuronService neuronService,
            IImageCacheService imageCacheService,
            ObservableCollection<ImageSource> photos)
        {
            this.logService = logService;
            this.networkService = networkService;
            this.neuronService = neuronService;
            this.imageCacheService = imageCacheService;
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
                    if (!this.networkService.IsOnline || !this.neuronService.Contracts.IsOnline)
                        continue;

                    Stream stream = await GetPhoto(attachment, now);
                    if (stream != null)
                    {
                        stream.Reset();
                        ImageSource imageSource = ImageSource.FromStream(() => stream); // Disposes the stream
                        Device.BeginInvokeOnMainThread(() => photos.Add(imageSource));
                    }
                }
                catch (Exception ex)
                {
                    this.logService.LogException(ex);
                }
            }
        }

        private async Task<Stream> GetPhoto(Attachment attachment, DateTime now)
        {
            // 1. Found in cache?
            if (this.imageCacheService.TryGet(attachment.Url, out Stream stream))
            {
                return stream;
            }

            // 2. Needs download
            KeyValuePair<string, TemporaryFile> pair = await this.neuronService.Contracts.GetContractAttachmentAsync(attachment.Url, Constants.Timeouts.DownloadFile);
            if (this.loadPhotosTimestamp > now)
            {
                pair.Value.Dispose();
                return null;
            }

            using (TemporaryFile file = pair.Value)
            {
                file.Reset();
                MemoryStream ms = new MemoryStream();
                await file.CopyToAsync(ms);
                // 3. Save to cache
                ms.Reset();
                await this.imageCacheService.Add(attachment.Url, ms);
                ms.Reset();
                return ms;
            }
        }
    }
}