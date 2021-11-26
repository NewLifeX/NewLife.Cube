using NewLife.Cube;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;
using Stardust;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

var star = new StarFactory(null, null, null);
TracerMiddleware.Tracer = star?.Tracer;

services.AddControllersWithViews();
services.AddCube();

var app = builder.Build();
app.UseCube(builder.Environment);
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default2", pattern: "{area=Admin}/{controller=Index}/{action=Index}/{id?}");

app.Run();