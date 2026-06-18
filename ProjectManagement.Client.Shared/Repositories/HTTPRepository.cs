using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories
{
    public class HTTPRepository(IHttpClientFactory factory)
    {
        private readonly HttpClient _httpClient = factory.CreateClient("Api");
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private static T EmptySuccessResult<T>()
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)true;

            if (!typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null)
                return default!;

            throw new InvalidOperationException("Empty response body.");
        }

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            // ❗ لا معالجة أخطاء هنا
            // Handlers قامت بكل شيء (Dialog / Redirect / TraceId)
            response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content is null)
                return EmptySuccessResult<T>();

            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return EmptySuccessResult<T>();

            if (typeof(T) == typeof(string))
                return (T)(object)body;

            try
            {
                var value = JsonSerializer.Deserialize<T>(body, JsonOptions);
                if (value is not null)
                    return value;
            }
            catch (JsonException) when (typeof(T) == typeof(bool) && bool.TryParse(body, out var parsedBool))
            {
                return (T)(object)parsedBool;
            }

            throw new InvalidOperationException($"Unable to deserialize response body to {typeof(T).Name}.");
        }

        public async Task<T> GetAsync<T>(string url, CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync(url, ct);
            return await ReadAsync<T>(response);
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(TRequest data, string url, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(url, data, cancellationToken: ct);
            return await ReadAsync<TResponse>(response);
        }

        public async Task<TResponse> PutAsync<TResponse, TRequest>(TRequest data, string url, CancellationToken ct = default)
        {
            var response = await _httpClient.PutAsJsonAsync(url, data, cancellationToken: ct);
            return await ReadAsync<TResponse>(response);
        }

        public Task<bool> PutAsync<T>(T data, string url, CancellationToken ct = default)
            => PutAsync<bool, T>(data, url, ct);

        public async Task<T> DeleteAsync<T>(string url, CancellationToken ct = default)
        {
            var response = await _httpClient.DeleteAsync(url, ct);
            return await ReadAsync<T>(response);
        }
        public Task<bool> DeleteAsync<T>(string url, T obj, CancellationToken ct = default) => DeleteAsync<bool, T>(url, obj, ct);
        public Task<bool> DeleteAsync(string url, CancellationToken ct = default)
            => DeleteAsync<bool>(url, ct);

        public async Task<T> DeleteAsync<T, TBody>(string url, TBody body, CancellationToken ct = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = JsonContent.Create(body, options: JsonOptions)
            };

            var response = await _httpClient.SendAsync(request, ct);
            return await ReadAsync<T>(response);
        }
    }
}
