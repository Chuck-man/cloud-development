using Ocelot.LoadBalancer.LoadBalancers;
using Ocelot.Responses;
using Ocelot.Values;

namespace Api.GateWay.LoadBalancers;

/// <summary>
/// Балансировщик нагрузки на основе параметров запроса
/// </summary>
/// <param name="services">Функция получения списка доступных сервисов</param>
public class WeightedRoundRobin(Func<Task<List<Service>>> services) : ILoadBalancer
{
    private readonly Func<Task<List<Service>>> _services = services;
    private readonly int[] _weights = [1, 2, 3, 2, 1];
    private int _currentIndex = -1;
    private int _remainingCalls = 0;

    public string Type => nameof(WeightedRoundRobin);

    private static readonly object _lock = new();

    public async Task<Response<ServiceHostAndPort>> LeaseAsync(HttpContext httpContext)
    {
        var services = await _services.Invoke();
        if (services == null || services.Count == 0)
            return new ErrorResponse<ServiceHostAndPort>(
                new ServicesAreEmptyError("No services available"));

        lock (_lock)
        {
            if (_currentIndex == -1 || _remainingCalls == 0)
            {
                _currentIndex = (_currentIndex + 1) % services.Count;
                _remainingCalls = _weights[_currentIndex];
            }

            var selectedService = services[_currentIndex];
            _remainingCalls--;

            return new OkResponse<ServiceHostAndPort>(selectedService.HostAndPort);
        }
    }

    public void Release(ServiceHostAndPort hostAndPort) {    }
}
