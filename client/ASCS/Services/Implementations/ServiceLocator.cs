using System;
using Microsoft.Extensions.DependencyInjection;

namespace MyASCS.Services.Implementations;

public abstract class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>()
               ?? throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
    }
}