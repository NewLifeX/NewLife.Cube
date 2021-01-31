using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.CubeUI.Models.Resp;
using NewLife.CubeUI.Helpers;

namespace NewLife.CubeUI.Services
{
    public interface IFieldService
    {
        Task<List<FieldResp>> GetDetailFieldsAsync(String baseUrl);
        Task<List<FieldResp>> GetEditFormFieldsAsync(String baseUrl);
        Task<List<FieldResp>> GetAddFormFieldsAsync(String baseUrl);
        Task<List<FieldResp>> GetListFieldsAsync(String baseUrl);
    }

    public class FieldService : IFieldService
    {
        private HttpClient _httpClient;

        public FieldService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="baseUrl">比如/Admin/User</param>
        /// <param name="kind"></param>
        /// <returns></returns>
        private async Task<List<FieldResp>> GetFieldsAsync(String baseUrl, String kind)
        {
            var data = await _httpClient.GetAsync<List<FieldResp>>($"{baseUrl}/GetEntityFields?kind={kind}");

            return data;
        }

        public Task<List<FieldResp>> GetDetailFieldsAsync(String baseUrl) => GetFieldsAsync(baseUrl, "Detail");

        public Task<List<FieldResp>> GetEditFormFieldsAsync(String baseUrl) => GetFieldsAsync(baseUrl, "EditForm");

        public Task<List<FieldResp>> GetAddFormFieldsAsync(String baseUrl) => GetFieldsAsync(baseUrl, "AddForm");

        public Task<List<FieldResp>> GetListFieldsAsync(String baseUrl) => GetFieldsAsync(baseUrl, "List");
    }
}