using System;
using System.Linq;
using System.Threading.Tasks;
using FaceIdentifier01Func.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ProjectOxford.Face;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace FaceIdentifier01Func
{
    public static class IdentifyPerson
    {
        [FunctionName("IdentifyPerson")]
        public static async Task Run(
            [QueueTrigger("facesidentified", Connection = Constants.StoreConnectionName)]string facesIdentifiedQueueItem,
            [Queue("failure", Connection = Constants.StoreConnectionName)] CloudQueue failureQueue,
            [Queue("personidentified", Connection = Constants.StoreConnectionName)] CloudQueue personIdentifiedQueueItem,
            TraceWriter log
        )
        {
            log.Info($"C# Queue trigger function processed: {facesIdentifiedQueueItem}");

            string subscriptionKey = Environment.GetEnvironmentVariable("FaceApi-SubscriptionKey");
            string personGroupId = Environment.GetEnvironmentVariable("FaceApi-PersonGroupId");

            var knownFaceResult = JsonConvert.DeserializeObject<KnownFaceResult>(facesIdentifiedQueueItem);

            if (knownFaceResult.KnownFace == null)
            {
                return;
            }

            IFaceServiceClient faceServiceClient = new FaceServiceClient(subscriptionKey, Constants.FaceApiRootUri);
            var knownPerson = await faceServiceClient.GetPersonInPersonGroupAsync(personGroupId, knownFaceResult.KnownFace.PersonId);

            var knownPersonInfo = new KnownPerson(knownPerson.Name, knownFaceResult.KnownFace.Confidence);
            var knownPersonResult = new KnownPersonResult(knownFaceResult.ImgId, knownPersonInfo);
            string msgJson = JsonConvert.SerializeObject(knownPersonResult);
            CloudQueueMessage msg = new CloudQueueMessage(msgJson);
            await personIdentifiedQueueItem.AddMessageAsync(msg);
        }
    }
}