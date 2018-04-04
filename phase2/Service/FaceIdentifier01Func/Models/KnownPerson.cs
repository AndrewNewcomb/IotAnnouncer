namespace FaceIdentifier01Func.Models
{
    public class KnownPerson
    {
        public string Name { get; }
        public double Confidence { get; }

        public KnownPerson(string name, double confidence)
        {
            Name = name;
            Confidence = confidence;
        }
    }
}