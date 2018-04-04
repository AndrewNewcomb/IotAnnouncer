namespace FaceIdentifier01Func.Models
{
    public class KnownPersonResult
    {
        public string ImgId { get; }
        public KnownPerson KnownPerson { get; }

        public KnownPersonResult(string imgId, KnownPerson knownPerson)
        {
            ImgId = imgId;
            KnownPerson = knownPerson;
        }
    }
}