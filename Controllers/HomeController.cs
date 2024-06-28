using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace AzureResizeImages.Controllers;

[Route("home")]
[ApiController]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<ActionResult> IndexAsync(IFormFile? image)
    {
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
           
            //using (var resizedStream = new MemoryStream())
            //{
            //    ResizeImage(stream, resizedStream, 200, 100);
            //    resizedStream.Position = 0;
            //    await blobClientResize.UploadAsync(resizedStream);
            //}
        }

        return Ok();
    }
    private void ResizeImage(Stream input, Stream output, int width, int height)
    {
        using (var image = Image.Load(input))
        {
            image.Mutate(x => x.Resize(width, height));
            image.Save(output, new JpegEncoder());
        }
    }
}
