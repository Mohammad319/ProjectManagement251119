using System.Net;
using System.Net.Http;
using ProjectManagement.Client.Shared.Repositories;
using ProjectManagement.Shared.DTO.Calculation.Template;
using Xunit;

namespace ProjectManagement.Tests;

public class HTTPRepositoryTests
{
    [Fact]
    public async Task GetAsync_EmptySuccessBody_ForNullableReferenceType_ReturnsNull()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        using var client = CreateClient(response);
        var repository = new HTTPRepository(new StubHttpClientFactory(client));

        var result = await repository.GetAsync<TemplateModelDTO?>("templates/getbyid/42");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_EmptySuccessBody_ForNonNullableValueType_Throws()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);
        using var client = CreateClient(response);
        var repository = new HTTPRepository(new StubHttpClientFactory(client));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAsync<int>("calc/value"));
    }

    // HttpClient owns (and disposes) the StubHandler; the response is disposed by the caller.
#pragma warning disable CA2000 // ownership of the handler transfers to the HttpClient (disposeHandler: true)
    private static HttpClient CreateClient(HttpResponseMessage response)
        => new(new StubHandler(response))
        {
            BaseAddress = new Uri("https://localhost/")
        };
#pragma warning restore CA2000

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
