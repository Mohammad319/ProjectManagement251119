using ProjectManagement.Client.Shared.Exception;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories
{
    public class HTTPRepository(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        private static async Task HandleError(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var reason = response.ReasonPhrase ?? "HTTP Error";
                var message = await response.Content.ReadAsStringAsync();
                throw new HttpResponseException(reason, message, response.StatusCode);
            }
        }
        private static async Task<R> ReturnResultOrThrowAsync<R>(HttpResponseMessage response)
        {
            await HandleError(response);
            return await response.Content.ReadFromJsonAsync<R>();
        }
        public async Task<R> PostAsync<R, T>(T data, string url)
        {
            var response = await _httpClient.PostAsJsonAsync(url, data);
            return await ReturnResultOrThrowAsync<R>(response);
        }

        public async Task<R> PutAsync<R, T>(T data, string url)
        {
            var response = await _httpClient.PutAsJsonAsync(url, data);
            return await ReturnResultOrThrowAsync<R>(response);
        }

        public Task<bool> PutAsync<T>(T data, string url) => PutAsync<bool, T>(data, url);

        public async Task<R> DeleteAsync<R>(string url)
        {
            var response = await _httpClient.DeleteAsync(url);
            return await ReturnResultOrThrowAsync<R>(response);
        }

        public Task<bool> DeleteAsync(string url) => DeleteAsync<bool>(url);

        public Task<bool> DeleteAsync<T>(string url, T obj) => DeleteAsync<bool, T>(url, obj);

        public async Task<R> DeleteAsync<R, T>(string url, T obj)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url, UriKind.Relative),
                Content = new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            return await ReturnResultOrThrowAsync<R>(response);
        }

        public async Task<R> GetAsync<R>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            return await ReturnResultOrThrowAsync<R>(response);
        }
    }
}
