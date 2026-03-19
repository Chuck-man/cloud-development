using Microsoft.Extensions.Caching.Distributed;
using Service.Api.Entities;
using System.Text.Json;

namespace Service.Api.Caching;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<Employee?> RetrieveFromCache(int id)
    {
        try
        {
            var json = await _cache.GetStringAsync(id.ToString());
            if (string.IsNullOrEmpty(json))
                return null;
            return JsonSerializer.Deserialize<Employee>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving employee {EmployeeId} from cache", id);
            return null;
        }
    }

    public async Task PopulateCache(Employee employee)
    {
        try
        {
            var json = JsonSerializer.Serialize(employee);
            await _cache.SetStringAsync(employee.Id.ToString(), json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                });
            _logger.LogDebug("Successfully cached employee {EmployeeId}", employee.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache employee {EmployeeId}", employee.Id);
        }
    }
}
