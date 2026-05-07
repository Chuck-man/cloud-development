using Amazon.SQS;
using Amazon.SQS.Model;
using Event.Sink.Storage;

namespace Event.Sink.Messaging;

/// <summary>
/// Клиентская служба для приема сррьщений из очереди SQS
/// </summary>
/// <param name="sqsClient">Клиент SQS</param>
/// <param name="scopeFactory">Фабрика контекста</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class SqsConsumerService(IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SqsConsumerService> logger) : BackgroundService
{
    private readonly string _queueUrl = configuration["AWS:Resources:SQSQueueUrl"]
        ?? throw new KeyNotFoundException("SQS queue name was not found in configuration");

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SQS consumer service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 5
                }, stoppingToken);

            if (response?.Messages == null || response.Messages.Count == 0)
            {
                logger.LogWarning("Received null or empty from {queue}", _queueUrl);
                continue;
            }

            logger.LogInformation("Received {count} messages", response.Messages.Count);

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(message, stoppingToken);
            }

            logger.LogInformation("Batch of {count} messages processed", response.Messages.Count);
        }
    }

    /// <summary>
    /// Сохраняет тело сообщения в объектное хранилище и удаляет сообщение из очереди
    /// </summary>
    /// <param name="message">Сообщение SQS</param>
    /// <param name="stoppingToken">Токен отмены</param>
    /// <returns></returns>
    private async Task ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Processing message: {messageId}", message.MessageId);

            using var scope = scopeFactory.CreateScope();
            var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
            var uploaded = await s3Service.UploadFile(message.Body);

            if (uploaded)
            {
                await sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, stoppingToken);
                logger.LogInformation("Message {messageId} processed and deleted", message.MessageId);
            }
            else
            {
                logger.LogWarning("UploadFile returned false for message {messageId}, deleting to avoid loop", message.MessageId);
                await SafeDeleteMessageAsync(message.ReceiptHandle, message.MessageId, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message: {messageId}", message.MessageId);
            await SafeDeleteMessageAsync(message.ReceiptHandle, message.MessageId, stoppingToken);
        }
    }

    /// <summary>
    /// Удаляет сообщение из очереди
    /// </summary>
    /// <param name="receiptHandle">Идентификатор получения сообщения выдаваемый SQS при чтении</param>
    /// <param name="messageId">Идентификатор сообщения</param>
    /// <param name="stoppingToken">Токен отмены</param>
    /// <returns></returns>
    private async Task SafeDeleteMessageAsync(string receiptHandle, string messageId, CancellationToken stoppingToken)
    {
        try
        {
            await sqsClient.DeleteMessageAsync(_queueUrl, receiptHandle, stoppingToken);
        }
        catch (Exception deleteEx)
        {
            logger.LogError(deleteEx, "Failed to delete message {messageId}", messageId);
        }
    }
}