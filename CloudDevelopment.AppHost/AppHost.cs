using Amazon;
using Aspire.Hosting.LocalStack.Container;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("employee-cache")
    .WithRedisInsight(containerName: "employee-insight");

var gateway = builder.AddProject<Projects.Api_GateWay>("api-gateway");

var awsConfig = builder.AddAWSSDKConfig()
    .WithProfile("default")
    .WithRegion(RegionEndpoint.EUCentral1);

var localStack = builder
    .AddLocalStack("employee-localstack", awsConfig: awsConfig, configureContainer: container =>
    {
        container.Lifetime = ContainerLifetime.Session;
        container.DebugLevel = 1;
        container.LogLevel = LocalStackLogLevel.Debug;
        container.Port = 4566;
        container.AdditionalEnvironmentVariables.Add("DEBUG", "1");
    });

var awsResources = builder.AddAWSCloudFormationTemplate("resources", "CloudFormation\\employee-template.yaml", "employee")
    .WithReference(awsConfig);

var minio = builder.AddMinioContainer("employee-minio");

for (var i = 0; i < 5; i++)
{
    var service = builder.AddProject<Projects.Service_Api>($"service-api-{i}", launchProfileName: null)
        .WithHttpsEndpoint(port: 15000 + i)
        .WithReference(cache, "RedisCache")
        .WithReference(awsResources)
        .WaitFor(cache)
        .WaitFor(awsResources);
    gateway.WaitFor(service);
}

var client = builder.AddProject<Projects.Client_Wasm>("employee")
    .WaitFor(gateway);

builder.AddProject<Projects.Event_Sink>("employee-sink")
    .WithReference(awsResources)
    .WithReference(minio)
    .WithEnvironment("AWS__Resources__MinioBucketName", "employee-bucket")
    .WaitFor(awsResources)
    .WaitFor(minio);

builder.UseLocalStack(localStack);

builder.Build().Run();
