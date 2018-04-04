using System;
using System.IO;
using System.Threading.Tasks;
using FaceIdentifier01Func.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace FaceIdentifier01Func
{
    public static class Identify
    {
        [FunctionName("Identify")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req,
            [Table("status", Connection = Constants.StoreConnectionName)] CloudTable statusTable,
            Binder binder,
            TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            if (req.ContentType != "application/octet-stream" || req.ContentLength == 0)
            {
                return new BadRequestObjectResult("Expected a jpeg image encoded as application/octet-stream");
            }
            
            var identifier = Guid.NewGuid().ToString();

            // add table entry so we can track overall status
            var overallResult = new OverallResult(identifier, null, null);
            TableOperation insertIdentificationResult = TableOperation.Insert(overallResult);
            await statusTable.ExecuteAsync(insertIdentificationResult);
            
            // store the image in a blob
            var bytes = GetImageBytes(req);
            await WriteImageToBlob(binder, identifier, bytes);

            return (ActionResult) new OkObjectResult(identifier);
        }

        private static async Task WriteImageToBlob(Binder binder, string identifier, byte[] bytes)
        {
            var attributes = new Attribute[]
            {
                new BlobAttribute($"raw-images/{identifier}", FileAccess.Write),
                new StorageAccountAttribute(Constants.StoreConnectionName)
            };

            using (var writer = await binder.BindAsync<Stream>(attributes).ConfigureAwait(false))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private static byte[] GetImageBytes(HttpRequest req)
        {
            var body = req.Body;
            var binaryReader = new BinaryReader(body);
            var bytes = binaryReader.ReadBytes((int) req.ContentLength);
            return bytes;
        }
    }
}
