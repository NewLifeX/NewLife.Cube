using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.CubeUI.Models.Resp;
using NewLife.CubeUI.Helpers;
using NewLife.CubeUI.Models.Entity;

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
            //var data = await _httpClient.PostAsync<List<Object>>($"{baseUrl}/Index");
            var data = new List<EntityBase>()
            {
                new User1()
                {
                    ID = 1,
                    Name = "Name1"
                },
                new User1()
                {
                    ID = 2,
                    Name = "Name2"
                }
            };
            return data;
        }
    }

    public class User1 : Entity<User1>
    {
        public int ID { get; set; }

        public string Name { get; set; }
    }
}