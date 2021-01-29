using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.CubeUI.Models.Resp;
using NewLife.CubeUI.Helpers;

namespace NewLife.CubeUI.Services
{
    public interface IMenuService
    {
        List<MenuResp> MenuTree { get; }

        Task GetAsync();
    }

    public class MenuService : IMenuService
    {
        private HttpClient _httpClient;

        public MenuService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public List<MenuResp> MenuTree { get; private set; }

        public async Task GetAsync()
        {
            var data = await _httpClient.GetAsync<List<MenuResp>>("/Admin/Index/GetMenuTree");
            MenuTree = data;
        }


    }
}
