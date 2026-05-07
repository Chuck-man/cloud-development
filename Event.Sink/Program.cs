using Amazon.SQS;
using Event.Sink.Messaging;
using Event.Sink.Storage;
using LocalStack.Client.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLocalStack(builder.Configuration);
builder.Services.AddAwsService<IAmazonSQS>();
builder.Services.AddHostedService<SqsConsumerService>();

builder.AddMinioClient("employee-minio");
builder.Services.AddScoped<IS3Service, S3MinioService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IS3Service>();
    await storage.EnsureBucketExists();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapControllers();
app.Run();