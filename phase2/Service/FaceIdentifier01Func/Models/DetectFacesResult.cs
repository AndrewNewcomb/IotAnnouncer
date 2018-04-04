using System;

namespace FaceIdentifier01Func.Models
{
    public class DetectFacesResult
    {
        public string ImgId { get; }
        public Guid[] FaceIds { get; }

        public DetectFacesResult(string imgId, Guid[] faceIds)
        {
            ImgId = imgId;
            FaceIds = faceIds;
        }
    }
}