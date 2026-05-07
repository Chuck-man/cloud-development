using Service.Api.Entities;

namespace Service.Api.Messaging;

/// <summary>
/// Интерфейс для отправки генерируемых сотрудников в брокер сообщений
/// </summary>
public interface IProducerService
{
    /// <summary>
    /// Отправляет сообщение в брокер
    /// </summary>
    /// <param name="employee">Сотрудник компании</param>
    public Task SendMessage(Employee employee);
}
