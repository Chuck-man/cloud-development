using Amazon.SQS;
using LocalStack.Client.Extensions;
using Service.Api.Caching;
using Service.Api.Generator;
using Service.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("RedisCache");

builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddScoped<IProducerService, ProducerService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/employee", (IEmployeeService service, int id) => service.ProcessEmployee(id));
app.Run();
