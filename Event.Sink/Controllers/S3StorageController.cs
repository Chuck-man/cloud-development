using Event.Sink.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json.Nodes;

namespace Event.Sink.Controllers;

/// <summary>
/// Контроллер для взаимодействия с S3
/// </summary>
/// <param name="storageService">Служба для работы с S3</param>
/// <param name="logger">Логгер</param>
[ApiController]
[Route("api/files")]
public class S3StorageController(IS3Service storageService, ILogger<S3StorageController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список хранящихся в S3 файлов
    /// </summary>
    /// <returns>Список ключей файлов</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<string>>> ListFiles()
    {
        logger.LogInformation("The {method} method of the {controller} controller has been called", nameof(ListFiles), nameof(S3StorageController));
        try
        {
            var list = await storageService.GetFileList();
            logger.LogInformation("Received a list of {count} files from the bucket", list.Count);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error when executing the {method} method of the {controller} controller", nameof(ListFiles), nameof(S3StorageController));
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Получает строковое представление хранящегося в S3 файла
    /// </summary>
    /// <param name="key">Ключ файла</param>
    /// <returns>Строковое представление файла</returns>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(JsonNode), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<JsonNode>> GetFile(string key)
    {
        logger.LogInformation("The {method} method of the {controller} controller has been called", nameof(GetFile), nameof(S3StorageController));
        try
        {
            var node = await storageService.DownloadFile(key);
            logger.LogInformation("Received JSON of {size} bytes", Encoding.UTF8.GetByteCount(node.ToJsonString()));
            return Ok(node);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Error occurred downloading"))
        {
            logger.LogWarning(ex, "The {key} file was not found", key);
            return NotFound($"File '{key}' not found");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error downloading the {key} file", key);
            return BadRequest(ex.Message);
        }
    }
}
