using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using NewLife.CubeUI.Helpers;
using NewLife.CubeUI.Models;
// using NewLife.CubeUI.Models.Req;
using NewLife.CubeUI.Models.Resp;

namespace NewLife.CubeUI.Services
{
    public interface IAccountService
    {
        User User { get; }
        Task Initialize();

        Task LoginAsync(LoginParamsType model);
        Task<string> GetCaptchaAsync(string modile);
    }

    public class AccountService : IAccountService
    {
        private string _userKey = "user";
        private readonly Random _random = new Random();
        private ILocalStorageService _localStorageService;
        private HttpClient _httpClient;

        private User _user;

        public AccountService(ILocalStorageService localStorageService, HttpClient httpClient)
        {
            _localStorageService = localStorageService;
            _httpClient = httpClient;
        }

        public User User
        {
            get
            {
                if (_user != null) return _user;

                //_user = _localStorageService.GetItem<User>(_userKey).Result;

                return _user;
            }

            private set { _user = value; }
        }

        public async Task Initialize()
        {
            User = await _localStorageService.GetItem<User>(_userKey);
        }

        public async Task LoginAsync(LoginParamsType model)
        {
            // var data = _client.Post<LoginResp>("/Admin/User/Login", model);
            var data = _httpClient.Post<LoginResp>("/Admin/User/Login", model);
            User = new User()
            {
                Token = data.Token
            };

            _localStorageService.SetItem(_userKey, User);
        }

        public Task<string> GetCaptchaAsync(string modile)
        {
            var captcha = _random.Next(0, 9999).ToString().PadLeft(4, '0');
            return Task.FromResult(captcha);
        }
    }
}