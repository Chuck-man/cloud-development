using Service.Api.Caching;
using Service.Api.Generator;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/employee", (IEmployeeService service, int id) => service.ProcessEmployee(id));
app.Run();
