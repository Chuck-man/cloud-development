using Amazon.SQS;
using Service.Api.Entities;
using System.Net;
using System.Text.Json;

namespace Service.Api.Messaging;

/// <summary>
/// Класс для отправки сообщений в брокер
/// </summary>
/// <param name="client">Клиент SQS</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class ProducerService(IAmazonSQS client, IConfiguration configuration, ILogger<ProducerService> logger) : IProducerService
{
    private readonly string _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
        ?? throw new KeyNotFoundException("SQS queue link was not found in configuration");

    private readonly int _maxRetries = 3;
    private readonly TimeSpan _initialDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public async Task SendMessage(Employee employee)
    {
        var delay = _initialDelay;

        for (var attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var json = JsonSerializer.Serialize(employee);
                var response = await client.SendMessageAsync(_queueUrl, json);

                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    logger.LogInformation("Employee {Id} sent to SQS queue (attempt {attempt})", employee.Id, attempt);
                    return;
                }

                logger.LogWarning("Send returned {statusCode} on attempt {attempt}", response.HttpStatusCode, attempt);
            }
            catch (Exception ex) when (attempt < _maxRetries)
            {
                logger.LogWarning(ex, "Attempt {attempt} failed, retrying...", attempt);
            }

            if (attempt < _maxRetries)
            {
                logger.LogDebug("Waiting {delay} before next retry", delay);
                await Task.Delay(delay);
                delay = delay * 2;
            }
        }

        logger.LogError("Failed to send employee {Id} after {maxRetries} attempts", employee.Id, _maxRetries);
    }

}
