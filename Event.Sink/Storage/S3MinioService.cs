using Minio;
using Minio.DataModel.Args;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace Event.Sink.Storage;

/// <summary>
/// Служба для манипуляции файлами в объектном хранилище Minio
/// </summary>
/// <param name="client">Minio клиент</param>
/// <param name="configuration">Конфигурация</param>
/// <param name="logger">Логгер</param>
public class S3MinioService(IMinioClient client, IConfiguration configuration, ILogger<S3MinioService> logger) : IS3Service
{
    private readonly string _bucketName = configuration["AWS:Resources:MinioBucketName"]
        ?? throw new KeyNotFoundException("Minio bucket name was not found in configuration");

    /// <inheritdoc/>
    public async Task<List<string>> GetFileList()
    {
        var list = new List<string>();
        var request = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithPrefix("")
            .WithRecursive(true);
        logger.LogInformation("Requesting a list of files in the bucket {bucket}", _bucketName);
        var responseList = client.ListObjectsEnumAsync(request);

        await foreach (var item in responseList)
        {
            if (item != null)
                list.Add(item.Key);
        }

        if (list.Count == 0)
            logger.LogWarning("The bucket {bucket} is empty or does not contain any objects", _bucketName);
        else
            logger.LogInformation("{count} files received from the {bucket} bucket", list.Count, _bucketName);

        return list;
    }

    /// <inheritdoc/>
    public async Task<bool> UploadFile(string fileData)
    {
        var rootNode = JsonNode.Parse(fileData) ?? throw new ArgumentException("Passed string is not a valid JSON");
        var id = rootNode["id"]?.GetValue<int>() ?? throw new ArgumentException("Passed JSON has invalid structure");

        var bytes = Encoding.UTF8.GetBytes(fileData);
        using var stream = new MemoryStream(bytes);
        stream.Seek(0, SeekOrigin.Begin);

        logger.LogInformation("Starting uploading the employee {file} to the bucket {bucket}", id, _bucketName);
        var request = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithStreamData(stream)
            .WithObjectSize(bytes.Length)
            .WithObject($"employee_{id}.json");

        try
        {
            var response = await client.PutObjectAsync(request);
            if (response.ResponseStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("Employee {Id} successfully uploaded to bucket {bucket}", id, _bucketName);
                return true;
            }

            logger.LogError("Couldn't upload employee {Id}: status {statusCode}", id, response.ResponseStatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error when uploading an employee {Id}", id);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<JsonNode> DownloadFile(string key)
    {
        logger.LogInformation("Starting downloading the {file} file from the {bucket} bucket", key, _bucketName);

        try
        {
            var memoryStream = new MemoryStream();

            var request = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(key)
                .WithCallbackStream(async (stream, cancellationToken) =>
                {
                    await stream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                });

            var response = await client.GetObjectAsync(request);

            if (response == null)
            {
                logger.LogError("Couldn't download the file {file}", key);
                throw new InvalidOperationException($"Error occurred downloading {key}");
            }
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            return JsonNode.Parse(reader.ReadToEnd()) ?? throw new InvalidOperationException("Downloaded document is not a valid JSON");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error when downloading a file {file}", key);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task EnsureBucketExists()
    {
        logger.LogInformation("Checking the existence of a bucket {bucket}", _bucketName);
        try
        {
            var request = new BucketExistsArgs()
                .WithBucket(_bucketName);

            var exists = await client.BucketExistsAsync(request);
            if (!exists)
            {
                logger.LogInformation("Creating bucket {bucket}", _bucketName);
                var createRequest = new MakeBucketArgs()
                    .WithBucket(_bucketName);
                await client.MakeBucketAsync(createRequest);
                return;
            }
            logger.LogInformation("Bucket {bucket} already exists", _bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled error when checking a bucket {bucket}", _bucketName);
            throw;
        }
    }
}