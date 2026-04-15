using NewLife.Cube;
using NewLife.Cube.ArcoVue;
using NewLife.Cube.React;
using NewLife.Cube.Services;
using NewLife.Cube.Swagger;
using NewLife.Cube.Vue;
using NewLife.Log;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 引入星尘，设置监控中间件
var star = services.AddStardust(null);
//TracerMiddleware.Tracer = star?.Tracer;

//services.AddHttpContextAccessor();

services.AddCubeFileStorage("Cube");

services.AddControllers();

services.AddCubeSwagger();

services.AddCube();


////注册短信服务（默认使用Cube的阿里云）
//services.AddSmsVerifyCode();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseCubeSwagger("swagger");
}

app.UseCube(builder.Environment);

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
app.MapControllers();

// UseVue 必须在 MapControllers 之后，确保 API endpoint 优先匹配，SPA 回退兜底
//app.UseVue(builder.Environment);
//app.UseReact(builder.Environment);
app.UseArcoVue(builder.Environment);

app.RegisterService("CubeDemo", null, builder.Environment.EnvironmentName, "/cube/info");

app.Run();
