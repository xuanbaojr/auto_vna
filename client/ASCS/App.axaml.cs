using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MyASCS.Services;
using MyASCS.Services.Implementations;
using MyASCS.Services.Interfaces;
using MyASCS.ViewModels;
using MyASCS.Views;

namespace MyASCS;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
            
        // Register services
        services.AddSingleton<IRtspStreamService, RtspStreamService>();
        services.AddSingleton<IPersonIdentificationService, PersonIdentificationService>();
        services.AddTransient<MainViewModel>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Store the service provider globally
        ServiceLocator.Initialize(serviceProvider);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var onboardingWindow = new OnboardingWindow();
            desktop.MainWindow = onboardingWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}