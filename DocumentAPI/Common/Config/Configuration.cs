using System.Net;
using System.Text.Json;
using Carter;
using DocumentAPI.Common.HttpClientFactory;
using DocumentAPI.Common.HttpClientFactory.Impl;
using DocumentAPI.Services;
using DocumentAPI.Services.External;
using Polly;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;

namespace DocumentAPI.Common.Config;

public static class Configuration
{
    public static IServiceCollection AddDependencyGroup(this IServiceCollection services)
    {
        services.AddScoped<ISecService, SecService>();
        services.AddScoped<ISecClientService, SecClientService>();
        services.AddSingleton<IHttpClientWrapper, HttpClientWrapper>();
        services.AddSingleton<IHttpClient, HttpClientBase>();
        return services;
    }

    public static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.ConfigureSwaggerGen(options => { options.AddEnumsWithValuesFixFilters(); });
        services.AddHttpContextAccessor();
        services.AddCarter();
        services.AddWebClientServices(config);
        return services;
    }

    public static void RegisterMiddlewares(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.UseSwagger()
                .UseSwaggerUI();

        app.UseHttpsRedirection();
    }

    private static IServiceCollection AddWebClientServices(this IServiceCollection services, IConfiguration config)
    {
        var clients = config.GetSection("WebClientConfig").Get<WebClientConfig>();
        foreach (var name in clients.Keys)
        {
            var clientConfig = clients[name];
            services.AddHttpClient(name, config =>
                {
                    config.BaseAddress = new Uri(clientConfig.BaseUrl);
                    config.Timeout = new TimeSpan(0, 0, clientConfig.TimeoutInSeconds);
                })
                .ConfigurePrimaryHttpMessageHandler(x => new HttpClientHandler() 
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                .AddTransientHttpErrorPolicy(p =>
                    p.WaitAndRetryAsync(clientConfig.RetryCnt, _ => TimeSpan.FromSeconds(2)))
                .AddTransientHttpErrorPolicy(policyBuilder => policyBuilder.CircuitBreakerAsync(
                    clientConfig.BeforeCircuitBreakerCnt,
                    TimeSpan.FromSeconds(clientConfig.DurationOfBreakInSeconds)));
            services.AddSingleton<IWebClientConfig>(sp => clients);
        }
    
        return services;
    }
}