using System;

namespace FaceIdentifier01Func.Models
{
    public class KnownFace
    {
        public Guid PersonId { get; }
        public double Confidence { get; }

        public KnownFace(Guid personId, double confidence)
        {
            PersonId = personId;
            Confidence = confidence;
        }
    }
}