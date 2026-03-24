using Service.Api.Caching;
using Service.Api.Entities;

namespace Service.Api.Generator;

/// <summary>
/// Служба для запуска юзкейса по обработке сотрудников компании
/// </summary>
/// <param name="cache">Кэш</param>
/// <param name="logger">Логгер</param>
public class EmployeeService(ICacheService cache, ILogger<EmployeeService> logger) : IEmployeeService
{
    /// <inheritdoc/>
    public async Task<Employee> ProcessEmployee(int id)
    {
        try
        {
            logger.LogInformation("Processing employee request for ID: {EmployeeId}", id);

            var employee = await cache.RetrieveFromCache(id);
            if (employee != null)
            {
                logger.LogInformation("Cache HIT for employee {EmployeeId}", id);
                return employee;
            }

            logger.LogInformation("Cache MISS for employee {EmployeeId}. Generating new data.", id);
            employee = EmployeeGenerator.Generate(id);
            logger.LogInformation("Populating the cache with employee {id}", id);

            _ = Task.Run(() => cache.PopulateCache(employee));

            return employee;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while processing employee {EmployeeId}", id);
            throw;
        }
    }
}
