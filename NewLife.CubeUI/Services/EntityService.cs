using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.CubeUI.Models.Resp;
using NewLife.CubeUI.Helpers;
using NewLife.CubeUI.Models.Entity;
using NewLife.Serialization;

namespace NewLife.CubeUI.Services
{
    public interface IEntityService
    {
        Task<List<EntityBase>> GetListAsync(String baseUrl);
    }

    public class EntityService : IEntityService
    {
        private HttpClient _httpClient;

        public EntityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl">比如/Admin/User</param>
        /// <returns></returns>
        public async Task<List<EntityBase>> GetListAsync(String baseUrl)
        {
            var result = await _httpClient.PostAsync<Object>($"{baseUrl}/Index") as List<Object>;
            
            var data = result?.Select(s
                => (EntityBase) new Entity().SetValues(s as Dictionary<String, Object>)).ToList();

            return data;
        }
    }
}