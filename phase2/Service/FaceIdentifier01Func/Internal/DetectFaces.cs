using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FaceIdentifier01Func.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace FaceIdentifier01Func.Internal
{
    public static class DetectFaces
    {
        [FunctionName("DetectFaces")]
        public static async Task Run(
            [BlobTrigger("raw-images/{name}", Connection = Constants.StoreConnectionName)]Stream myBlob, 
            string name,
            [Queue("failure", Connection = Constants.StoreConnectionName)] CloudQueue failureQueue,
            [Queue("facesdetected", Connection = Constants.StoreConnectionName)] CloudQueue facesDetectedQueue,
            [Table("status", Connection = Constants.StoreConnectionName)] CloudTable statusTable,
            TraceWriter log
            )
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var response = await CallFaceApiToDetectFaces(myBlob);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                await AddMessageAsync(failureQueue, new Failure(name, response.StatusCode));
                return;
            }

            string contentString = await response.Content.ReadAsStringAsync();
            var faces = JsonConvert.DeserializeObject<Face[]>(contentString);

            var overallResult = new OverallResult(name, faces.Length, null);
            TableOperation insertOrMergeIdentificationResult = TableOperation.InsertOrReplace(overallResult);
            await statusTable.ExecuteAsync(insertOrMergeIdentificationResult);

            if (faces.Length > 0)
            {
                var detectFacesResult = new DetectFacesResult(name, faces.Select(x => x.FaceId).ToArray());
                await AddMessageAsync(facesDetectedQueue, detectFacesResult);
            }
        }

        private static async Task AddMessageAsync(CloudQueue queue, object objectToSend)
        {
            string facesJson = JsonConvert.SerializeObject(objectToSend);
            CloudQueueMessage facesMsg = new CloudQueueMessage(facesJson);
            await queue.AddMessageAsync(facesMsg);
        }

        private static async Task<HttpResponseMessage> CallFaceApiToDetectFaces(Stream myBlob)
        {
            HttpClient client = new HttpClient();

            string subscriptionKey = Environment.GetEnvironmentVariable("FaceApi-SubscriptionKey");
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            //string requestParameters = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";
            string requestParameters = "returnFaceId=true&returnFaceLandmarks=false";
            string uri = Constants.FaceApiRootUri + "/detect?" + requestParameters;

            var binaryReader = new BinaryReader(myBlob);
            byte[] byteData = binaryReader.ReadBytes((int) myBlob.Length);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                HttpResponseMessage response = await client.PostAsync(uri, content);
                return response;
            }
        }
    }
}
