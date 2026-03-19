using Service.Api.Generator;
using Service.Api.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(["http://localhost:5127", "https://localhost:7282"]);
    policy.WithMethods("GET");
    policy.WithHeaders("Content-Type");
}));

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/employee", (IEmployeeService service, int id) => service.ProcessEmployee(id));
app.UseCors();
app.Run();
