using StackExchange.Redis;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var gateway = builder.AddProject<Projects.Api_GateWay>("api-gateway");

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WaitFor(cache);
    gateway.WaitFor(service);
}

var client = builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.Build().Run();
