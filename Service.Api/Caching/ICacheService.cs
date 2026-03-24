using Service.Api.Entities;

namespace Service.Api.Caching;

/// <summary>
/// Интерфейс для работы с кэшем сотрудников компании 
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Пытается достать сотрудника из кэша
    /// </summary>
    /// <param name="id">Идентификатор</param>
    /// <returns>Сотрудника компании или null</returns>
    public Task<Employee?> RetrieveFromCache(int id);

    /// <summary>
    /// Кладёт сотрудника в кэш
    /// </summary>
    /// <param name="employee">Сотрудник компании</param>
    /// <returns></returns>
    public Task PopulateCache(Employee employee);
}
