using Microsoft.WindowsAzure.Storage.Table;

namespace FaceIdentifier01Func.Models
{
    public class OverallResult : TableEntity
    {
        public string ImgId { get; }

        public int? FaceCount { get; set; }

        public int? KnownFaceCount { get; set; }

        public OverallResult(string imgId, int? faceCount, int? knownFaceCount)
        {
            PartitionKey = "status";
            RowKey = imgId;
            ImgId = imgId;

            FaceCount = faceCount;
            KnownFaceCount = knownFaceCount;
        }
    }
}