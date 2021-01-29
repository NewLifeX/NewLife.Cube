namespace NewLife.CubeUI.Models.Resp
{
    public class LoginRespModel : ModelRespBase
    {
        public LoginResp Data { get; set; }
    }

    public class LoginResp
    {
        public string Token { get; set; }
    }
}