using System;
using System.Net;

namespace FaceIdentifier01Func.Models
{
    public class Failure
    {
        public string Name { get; private set; }
        public HttpStatusCode HttpStatusCode { get; private set; }
        public string ExnMessage { get; private set; }

        public Failure(string name, HttpStatusCode httpStatusCode)
        {
            Name = name;
            HttpStatusCode = httpStatusCode;
        }

        public Failure(string name, Exception ex)
        {
            Name = name;
            ExnMessage = ex.Message;
            HttpStatusCode = HttpStatusCode.InternalServerError;
        }
    }
}