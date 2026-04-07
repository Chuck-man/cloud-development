using Api.GateWay.LoadBalancers;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot()
    .AddCustomLoadBalancer<WeightedRoundRobin>((_, _, provider) => new(provider.GetAsync));

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.WithOrigins(["http://localhost:5127", "https://localhost:7282"]);
    policy.WithMethods("GET");
    policy.WithHeaders("Content-Type");
}));

var app = builder.Build();

app.UseCors();

await app.UseOcelot();

app.Run();
