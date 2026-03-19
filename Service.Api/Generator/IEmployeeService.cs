using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Интерфейс для запуска юзкейса по обработке сотрудников компании
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Обработка запроса на генерацию сотрудника компании
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <returns>Сотрудник компании</returns>
    public Task<Employee> ProcessEmployee(int id);
}
