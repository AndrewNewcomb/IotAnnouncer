using System;
using System.Linq;
using System.Threading.Tasks;
using FaceIdentifier01Func.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ProjectOxford.Face;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace FaceIdentifier01Func.Internal
{
    public static class IdentifyKnownFaces
    {
        [FunctionName("IdentifyKnownFaces")]
        public static async Task Run(
            [QueueTrigger("facesdetected", Connection = Constants.StoreConnectionName)]string detectedFacesQueueItem,
            [Queue("failure", Connection = Constants.StoreConnectionName)] CloudQueue failureQueue,
            [Queue("facesidentified", Connection = Constants.StoreConnectionName)] CloudQueue facesIdentifiedQueue,
            [Table("status", Connection = Constants.StoreConnectionName)] CloudTable statusTable,
            TraceWriter log
        )
        {
            log.Info($"C# Queue trigger function processed: {detectedFacesQueueItem}");

            string subscriptionKey = Environment.GetEnvironmentVariable("FaceApi-SubscriptionKey");
            string personGroupId = Environment.GetEnvironmentVariable("FaceApi-PersonGroupId");

            var facesResult = JsonConvert.DeserializeObject<DetectFacesResult>(detectedFacesQueueItem);

            IFaceServiceClient faceServiceClient = new FaceServiceClient(subscriptionKey, Constants.FaceApiRootUri);
            var identifiedFaces = await faceServiceClient.IdentifyAsync(facesResult.FaceIds, personGroupId);

            var knownFaces = identifiedFaces
                .Select(f => f.Candidates.Any() ? f.Candidates.First() : null)
                .Where(c => c != null)
                .Select(c => new KnownFace(c.PersonId, c.Confidence))
                .ToArray();
                
            await UpdateOverallStatus(statusTable, facesResult, knownFaces);

            foreach (var knownFace in knownFaces)
            {
                var knownFaceResult = new KnownFaceResult(facesResult.ImgId, knownFace);
                string msgJson = JsonConvert.SerializeObject(knownFaceResult);
                CloudQueueMessage msg = new CloudQueueMessage(msgJson);
                await facesIdentifiedQueue.AddMessageAsync(msg);
            }
        }

        private static async Task UpdateOverallStatus(
            CloudTable statusTable, 
            DetectFacesResult facesResult,
            KnownFace[] knownFaces)
        {
            var overallResult = new OverallResult(facesResult.ImgId, facesResult.FaceIds.Length, knownFaces.Length);
            TableOperation insertOrMergeIdentificationResult = TableOperation.InsertOrReplace(overallResult);
            await statusTable.ExecuteAsync(insertOrMergeIdentificationResult);
        }
    }
}