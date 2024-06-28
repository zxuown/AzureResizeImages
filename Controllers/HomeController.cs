using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace AzureResizeImages.Controllers
{
    [Route("home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;

        public HomeController(IConfiguration configuration, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _telemetryClient = telemetryClient;
        }

        [HttpPost]
        public async Task<ActionResult> IndexAsync(IFormFile? image)
        {
            if (image == null)
            {
                _telemetryClient.TrackEvent("Image upload failed: No file provided.");
                return BadRequest("No image provided.");
            }

            var connectionString = _configuration.GetValue<string>("Azure:BlobStorageDefaultImages:ConnectionString");
            string imagePath = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            string containerName = "defaultimages";
            string containerNameResize = "defaultimagesresize";
            BlobContainerClient containerClient;
            BlobContainerClient containerClientResize;

            if (blobServiceClient.GetBlobContainers().Any(x => x.Name == containerName) &&
                blobServiceClient.GetBlobContainers().Any(x => x.Name == containerNameResize))
            {
                containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                containerClientResize = blobServiceClient.GetBlobContainerClient(containerNameResize);
            }
            else
            {
                containerClient = blobServiceClient.CreateBlobContainer(containerName);
                containerClientResize = blobServiceClient.CreateBlobContainer(containerNameResize);
            }

            var blobClient = containerClient.GetBlobClient(imagePath);
            var blobClientResize = containerClientResize.GetBlobClient(imagePath);
            try
            {
                using (var stream = image.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream);
                    stream.Position = 0;
                    using (var resizedStream = new MemoryStream())
                    {
                        using (var imageToSave = Image.Load(stream))
                        {
                            imageToSave.Mutate(x => x.Resize(200, 100));
                            imageToSave.Save(resizedStream, new JpegEncoder());
                        }
                        resizedStream.Position = 0;
                        await blobClientResize.UploadAsync(resizedStream);
                    }
                }

                _telemetryClient.TrackEvent("Image uploaded and resized successfully");
                return Ok();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
