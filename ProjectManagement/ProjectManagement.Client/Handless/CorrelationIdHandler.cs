namespace ProjectManagement.Client.Handless
{
    public class CorrelationIdHandler : DelegatingHandler
    {
        private const string HeaderName = "X-Correlation-ID";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // أضف CorrelationId إذا لم يكن موجوداً
            if (!request.Headers.Contains(HeaderName))
                request.Headers.TryAddWithoutValidation(HeaderName, Guid.NewGuid().ToString("N"));

            return base.SendAsync(request, ct);
        }
    }
}
