using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.Configuration;
using ImageAIRenamer.Infrastructure.Logging;
using ImageAIRenamer.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ImageAIRenamer.Infrastructure.DependencyInjection;

/// <summary>
/// Extension methods for configuring dependency injection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application services to the service collection
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IServiceProvider>(sp => sp);
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IGeminiService, GeminiService>();
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();

        // ViewModels
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<ImageSearchViewModel>();
        services.AddTransient<ImageRenameViewModel>();
        services.AddTransient<MainViewModel>();

        return services;
    }
}
