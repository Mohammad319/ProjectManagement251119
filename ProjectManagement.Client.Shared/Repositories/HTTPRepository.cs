using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories
{
    public class HTTPRepository(IHttpClientFactory factory)
    {
        private readonly HttpClient _httpClient = factory.CreateClient("Api");

        private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        {
            // ❗ لا معالجة أخطاء هنا
            // Handlers قامت بكل شيء (Dialog / Redirect / TraceId)
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>()
                   ?? throw new InvalidOperationException("Empty response body.");
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            return await ReadAsync<T>(response);
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(TRequest data, string url)
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);
            return await ReadAsync<TResponse>(response);
        }

        public async Task<TResponse> PutAsync<TResponse, TRequest>(TRequest data, string url)
        {
            var response = await _httpClient.PutAsJsonAsync(url, data);
            return await ReadAsync<TResponse>(response);
        }

        public Task<bool> PutAsync<T>(T data, string url)
            => PutAsync<bool, T>(data, url);

        public async Task<T> DeleteAsync<T>(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            return await ReadAsync<T>(response);
        }
        public Task<bool> DeleteAsync<T>(string url, T obj) => DeleteAsync<bool, T>(url, obj);
        public Task<bool> DeleteAsync(string url)
            => DeleteAsync<bool>(url);

        public async Task<T> DeleteAsync<T, TBody>(string url, TBody body)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            return await ReadAsync<T>(response);
        }
    }
}
