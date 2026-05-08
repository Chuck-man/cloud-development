using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Service.Api.Entities;
using System.Text.Json;
using Xunit.Abstractions;
using System.Collections.Concurrent;

namespace Aspire.AppHost.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна:
/// API -> SQS -> Event.Sink -> MinIO
/// </summary>
/// <param name="output">Служба журналирования юнит-тестов</param>
public class IntegrationTests(ITestOutputHelper output) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private DistributedApplication? _app;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        var cancellationToken = CancellationToken.None;
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CloudDevelopment_AppHost>(cancellationToken);

        builder.Configuration["DcpPublisher:RandomizePorts"] = "false";
        builder.Services.AddLogging(logging =>
        {
            logging.AddXUnit(output);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting.Dcp", LogLevel.Debug);
            logging.AddFilter("Aspire.Hosting", LogLevel.Debug);
        });

        _app = await builder.BuildAsync(cancellationToken);
        await _app.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Основной тест: запрос сотрудника через gateway -> файл появляется в Minio,
    /// данные в ответе API и в объектном хранилище совпадают.
    /// </summary>
    [Fact]
    public async Task EmployeePipeline_ApiToStorage_Success()
    {
        var id = new Random().Next(1, 100);

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");
        using var gatewayResponse = await gatewayClient.GetAsync($"/employee?id={id}");
        gatewayResponse.EnsureSuccessStatusCode();
        var apiEmployee = JsonSerializer.Deserialize<Employee>(
            await gatewayResponse.Content.ReadAsStringAsync(), _jsonOptions);

        await Task.Delay(TimeSpan.FromSeconds(5));

        using var storageClient = _app!.CreateHttpClient("employee-sink", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        listResponse.EnsureSuccessStatusCode();
        var fileList = JsonSerializer.Deserialize<List<string>>(
            await listResponse.Content.ReadAsStringAsync());

        using var fileResponse = await storageClient.GetAsync($"/api/files/employee_{id}.json");
        fileResponse.EnsureSuccessStatusCode();
        var s3Employee = JsonSerializer.Deserialize<Employee>(
            await fileResponse.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(fileList);
        Assert.Single(fileList);
        Assert.Equal($"employee_{id}.json", fileList![0]);

        Assert.NotNull(apiEmployee);
        Assert.NotNull(s3Employee);
        Assert.Equal(id, s3Employee!.Id);
        Assert.Equivalent(apiEmployee, s3Employee);
    }

    /// <summary>
    /// Проверка устойчивости: некорректный запрос (без id) не попадает в очередь
    /// и не создаёт мусорных файлов.
    /// </summary>
    [Fact]
    public async Task InvalidRequest_DoesNotCreateFile()
    {
        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");

        using var badResponse = await gatewayClient.GetAsync("/employee");
        Assert.False(badResponse.IsSuccessStatusCode);

        await Task.Delay(TimeSpan.FromSeconds(3));

        using var storageClient = _app!.CreateHttpClient("employee-sink", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        listResponse.EnsureSuccessStatusCode();
        var files = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(files);
        Assert.DoesNotContain(files!, f => f.StartsWith("employee_0") || f.Equals("employee_.json"));
    }

    /// <summary>
    /// Параллельная отправка нескольких сотрудников: все файлы должны быть созданы,
    /// содержимое совпадает с ответами API.
    /// </summary>
    [Fact]
    public async Task ConcurrentRequests_AllEmployeesStored()
    {
        const int concurrentCount = 5;
        var ids = Enumerable.Range(1, concurrentCount).Select(_ => new Random().Next(200, 300)).Distinct().ToArray();
        var results = new ConcurrentDictionary<int, Employee?>();

        using var gatewayClient = _app!.CreateHttpClient("api-gateway", "http");

        var tasks = ids.Select(async id =>
        {
            using var response = await gatewayClient.GetAsync($"/employee?id={id}");
            response.EnsureSuccessStatusCode();
            var emp = JsonSerializer.Deserialize<Employee>(
                await response.Content.ReadAsStringAsync(), _jsonOptions);
            results[id] = emp;
        });
        await Task.WhenAll(tasks);

        await Task.Delay(TimeSpan.FromSeconds(8));

        using var storageClient = _app!.CreateHttpClient("employee-sink", "http");
        using var listResponse = await storageClient.GetAsync("/api/files");
        listResponse.EnsureSuccessStatusCode();
        var fileList = JsonSerializer.Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);
        foreach (var id in ids)
        {
            var expectedFileName = $"employee_{id}.json";
            Assert.Contains(expectedFileName, fileList!);

            using var fileResponse = await storageClient.GetAsync($"/api/files/{expectedFileName}");
            fileResponse.EnsureSuccessStatusCode();
            var storedEmployee = JsonSerializer.Deserialize<Employee>(
                await fileResponse.Content.ReadAsStringAsync(), _jsonOptions);

            Assert.NotNull(storedEmployee);
            Assert.Equal(id, storedEmployee!.Id);
            Assert.True(results.TryGetValue(id, out var apiEmployee));
            Assert.Equivalent(apiEmployee, storedEmployee);
        }
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}