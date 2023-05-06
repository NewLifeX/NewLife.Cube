using System.Runtime.Loader;
using NewLife.Cube;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 引入星尘，设置监控中间件
var star = services.AddStardust(null);
TracerMiddleware.Tracer = star?.Tracer;

// 启用接口响应压缩
services.AddResponseCompression();

services.AddControllersWithViews();
services.AddCube();

var app = builder.Build();
app.UseStaticFiles();

app.UseCube(builder.Environment);
app.UseCubeHome();

app.UseAuthorization();
app.UseResponseCompression();
//app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
//app.MapControllerRoute(name: "default2", pattern: "{area=Admin}/{controller=Index}/{action=Index}/{id?}");

app.RegisterService("SSO", null, builder.Environment.EnvironmentName, "/cube/info");

AssemblyLoadContext.Default.Unloading += ctx =>
{
    XTrace.WriteLine("Unloading!");
};
AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    XTrace.WriteLine("ProcessExit!");
};
Console.CancelKeyPress += (s, e) =>
{
    XTrace.WriteLine("CancelKeyPress!");
};

app.Run();

XTrace.WriteLine("Finish!");
Thread.Sleep(3000);
XTrace.WriteLine("Exit!");