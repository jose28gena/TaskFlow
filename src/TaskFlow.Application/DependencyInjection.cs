using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Services;

namespace TaskFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<TaskService>();
        return services;
    }
}
