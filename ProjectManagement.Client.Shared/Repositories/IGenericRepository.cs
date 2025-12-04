using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectManagement.Client.Shared.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync(string endpoint);
        Task<R> PostAsync<R>(string endpoint,T data);
        Task<bool> UpdateAsync(string endpoint, T entity);
        Task<bool> DeleteAsync(string endpoint);
    }
    public class GenericRepository<T>(HTTPRepository _httpRepository) : IGenericRepository<T> where T : class
    {
        public async Task<List<T>> GetAllAsync(string endpoint)
        {
            return await _httpRepository.GetAsync<List<T>>(endpoint) ?? [];
        }
        public async Task<R> PostAsync<R>(string endpoint,T data)
        {
            return await _httpRepository.PostAsync<R, T>(data, endpoint);
        }
        public async Task<bool> UpdateAsync(string endpoint, T entity)
        {
            return await _httpRepository.PutAsync(entity, endpoint);
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            return await _httpRepository.DeleteAsync(endpoint);
        }
    }

}
