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
        var repository = CreateRepository(new HttpResponseMessage(HttpStatusCode.OK));

        var result = await repository.GetAsync<TemplateModelDTO?>("templates/getbyid/42");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_EmptySuccessBody_ForNonNullableValueType_Throws()
    {
        var repository = CreateRepository(new HttpResponseMessage(HttpStatusCode.OK));

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAsync<int>("calc/value"));
    }

    private static HTTPRepository CreateRepository(HttpResponseMessage response)
    {
        var client = new HttpClient(new StubHandler(response))
        {
            BaseAddress = new Uri("https://localhost/")
        };

        return new HTTPRepository(new StubHttpClientFactory(client));
    }

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
