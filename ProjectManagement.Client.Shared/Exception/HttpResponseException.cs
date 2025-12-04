using System.Net;

namespace ProjectManagement.Client.Shared.Exception
{
    public class HttpResponseException(string reason, string message, HttpStatusCode statusCode)
        : System.Exception(message)
    {
        public string Text { get; init; } = message;
        public string Reason { get; init; } = reason;
        public HttpStatusCode HttpStatusCode { get; init; } = statusCode;
    }
}