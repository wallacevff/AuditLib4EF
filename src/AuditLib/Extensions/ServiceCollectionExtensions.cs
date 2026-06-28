using AuditLib.Abstractions;
using AuditLib.Infrastructure;
using AuditLib.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuditLib.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddAuditLib(
        this IServiceCollection services,
        Action<AuditLibOptions>? configureOptions = null)
    {
        if (configureOptions == null)
            return services;
        var options = AuditLibOptions.Default;
        configureOptions(options);

        return services.AddAuditLibCore(options);
    }

    public static IServiceCollection AddAuditLib(
        this IServiceCollection services,
        Action<AuditConfigurationBuilder> configure)
    {
        var builder = new AuditConfigurationBuilder();
        configure(builder);
        return services.AddAuditLibCore(builder.Build());
    }

    public static IServiceCollection AddAuditLibWithInterceptor(
        this IServiceCollection services,
        Action<AuditConfigurationBuilder> configure)
    {
        return services.AddAuditLibWithInterceptorCore(configure.BuildFromBuilder());
    }

    public static IServiceCollection AddAuditLibWithInterceptor(
        this IServiceCollection services,
        Action<AuditLibOptions>? configureOptions = null)
    {
        return configureOptions == null
            ? services.AddAuditLibWithInterceptorCore(AuditLibOptions.Default)
            : services.AddAuditLibWithInterceptorCore(ApplyOptions(configureOptions));
    }

    private static AuditLibOptions ApplyOptions(Action<AuditLibOptions> configure)
    {
        var options = AuditLibOptions.Default;
        configure(options);
        return options;
    }

    private static AuditLibOptions BuildFromBuilder(this Action<AuditConfigurationBuilder> configure)
    {
        var builder = new AuditConfigurationBuilder();
        configure(builder);
        return builder.Build();
    }

    private static IServiceCollection AddAuditLibCore(this IServiceCollection services, AuditLibOptions options)
    {
        services.AddSingleton(options);
        services.TryAddScoped<IUserContext>(sp =>
        {
            var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return new DefaultUserContext(() =>
                {
                    var user = httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst("user_id")?.Value;
                    if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var id))
                        return id;
                    if (httpContextAccessor.HttpContext?.Items is { } items &&
                        items.TryGetValue("CurrentUserId", out var currentUserId) &&
                        currentUserId is Guid uid)
                        return uid;
                    var subClaim = user?.FindFirst("sub")?.Value;
                    if (subClaim is not null && Guid.TryParse(subClaim, out var subId))
                        return subId;
                    return null;
                });
            }
            return new DefaultUserContext(() => null);
        });
        services.TryAddScoped<AuditInterceptor>();
        services.TryAddScoped<IAuditLogRepository>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            return new AuditLogRepository(dbContext);
        });
        services.TryAddScoped<IAuditLogService, AuditLogService>();
        return services;
    }

    private static IServiceCollection AddAuditLibWithInterceptorCore(this IServiceCollection services, AuditLibOptions options)
    {
        services.AddSingleton(options);
        services.TryAddScoped(sp =>
        {
            var userContext = sp.GetService<IUserContext>();
            return new AuditInterceptor(options, userContext);
        });
        services.TryAddScoped<IUserContext>(sp =>
        {
            var httpContextAccessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return new DefaultUserContext(() =>
                {
                    var user = httpContextAccessor.HttpContext?.User;
                    var userIdClaim = user?.FindFirst("user_id")?.Value;
                    if (userIdClaim is not null && Guid.TryParse(userIdClaim, out var id))
                        return id;
                    if (httpContextAccessor.HttpContext?.Items is { } items &&
                        items.TryGetValue("CurrentUserId", out var currentUserId) &&
                        currentUserId is Guid uid)
                        return uid;
                    var subClaim = user?.FindFirst("sub")?.Value;
                    if (subClaim is not null && Guid.TryParse(subClaim, out var subId))
                        return subId;
                    return null;
                });
            }
            return new DefaultUserContext(() => null);
        });
        services.TryAddScoped<IAuditLogRepository>(sp =>
        {
            var dbContext = sp.GetRequiredService<DbContext>();
            return new AuditLogRepository(dbContext);
        });
        services.TryAddScoped<IAuditLogService, AuditLogService>();
        return services;
    }

    public static DbContextOptionsBuilder UseAuditInterceptor(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
        builder.AddInterceptors(interceptor);
        return builder;
    }
}
