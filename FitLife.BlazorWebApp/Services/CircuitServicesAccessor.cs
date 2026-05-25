using Microsoft.AspNetCore.Components.Server.Circuits;

namespace FitLife.BlazorWebApp.Services;

/// <summary>
/// Provides access to the current circuit ID for session management
/// </summary>
public class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider?> _currentServiceProvider = new();

    public IServiceProvider? Services
    {
        get => _currentServiceProvider.Value;
        set => _currentServiceProvider.Value = value;
    }
}

/// <summary>
/// Circuit handler that sets the service provider for the current circuit
/// </summary>
public class ServicesAccessorCircuitHandler : CircuitHandler
{
    private readonly CircuitServicesAccessor _servicesAccessor;
    private readonly IServiceProvider _serviceProvider;

    public ServicesAccessorCircuitHandler(CircuitServicesAccessor servicesAccessor, IServiceProvider serviceProvider)
    {
        _servicesAccessor = servicesAccessor;
        _serviceProvider = serviceProvider;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            _servicesAccessor.Services = _serviceProvider;
            await next(context);
        };
    }
}
