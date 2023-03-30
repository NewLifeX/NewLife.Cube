using NewLife.Cube;
using NewLife.Cube.WebMiddleware;
using NewLife.Log;

XTrace.UseConsole();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// 引入星尘，设置监控中间件
var star = services.AddStardust(null);
TracerMiddleware.Tracer = star?.Tracer;

//services.AddHttpContextAccessor();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 解决 NewLife.Setting 与 XCode.Setting 冲突的问题
    options.CustomSchemaIds(type => type.FullName);
});

services.AddCube();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCube(builder.Environment);

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Index}/{action=Index}/{id?}");
app.MapControllers();

app.RegisterService("CubeDemo", null, builder.Environment.EnvironmentName, "/cube/info");

app.Run();
