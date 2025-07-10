using NewLife.Cube;
using NewLife.Cube.Swagger;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 引入星尘，设置监控中间件
var star = services.AddStardust(null);
TracerMiddleware.Tracer = star?.Tracer;

//services.AddHttpContextAccessor();

services.AddControllers();

services.AddCubeSwagger();

services.AddCube();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseCubeSwagger();
}

app.UseCube(builder.Environment);

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
app.MapControllers();

app.RegisterService("CubeDemo", null, builder.Environment.EnvironmentName, "/cube/info");

app.Run();
