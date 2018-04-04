namespace FaceIdentifier01Func.Models
{
    public class KnownFaceResult
    {
        public string ImgId { get; }
        public KnownFace KnownFace { get; }

        public KnownFaceResult(string imgId, KnownFace knownFace)
        {
            ImgId = imgId;
            KnownFace = knownFace;
        }
    }
}