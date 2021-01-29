using System;
using System.Threading.Tasks;
using AntDesign;
using BlazorApp.Helpers;
using Microsoft.AspNetCore.Components;
using NewLife.CubeUI.Models;
using NewLife.CubeUI.Services;

namespace NewLife.CubeUI.Pages.User.Login
{
    public partial class Login
    {
        private readonly LoginParamsType _model = new LoginParamsType();

        [Inject] public NavigationManager NavigationManager { get; set; }

        [Inject] public IAccountService AccountService { get; set; }

        [Inject] public MessageService Message { get; set; }

        public async Task HandleSubmit()
        {
            try
            {
                await AccountService.LoginAsync(_model);

                var returnUrl = NavigationManager.QueryString("returnUrl") ?? "/";
                Console.WriteLine($"Navigate To {returnUrl}");
                NavigationManager.NavigateTo(returnUrl);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);

                // 错误提示
                //AlertService.Error(ex.Message);
                //loading = false;
                StateHasChanged();
            }
        }

        public async Task GetCaptcha()
        {
            var captcha = await AccountService.GetCaptchaAsync(_model.Mobile);
            await Message.Success($"获取验证码成功！验证码为：{captcha}");
        }
    }
}