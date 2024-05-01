using System.Net;
using System.Text.Json;
using Carter;
using DocumentAPI.Common.HttpClientFactory;
using DocumentAPI.Common.HttpClientFactory.Impl;
using DocumentAPI.DTO.Mapper;
using DocumentAPI.Infrastructure.Repository;
using DocumentAPI.Service;
using DocumentAPI.Service.Integration.SEC;
using Polly;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;

namespace DocumentAPI.Common.Config;

public static class ServiceInitializer
{
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
        services.AddAutoMapper(typeof(AutoMapperProfile));
        return services;
    }
    
    public static IServiceCollection AddDependencyGroup(this IServiceCollection services)
    {
        services.AddScoped<ISecService, SecService>();
        services.AddScoped<ISecClientService, SecClientService>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddSingleton<IHttpClientWrapper, HttpClientWrapper>();
        services.AddSingleton<IHttpClient, HttpClientBase>();
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
                    TimeSpan.FromSeconds(clientConfig.DurationOfBreakInSeconds)))
                .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests) // Retry on TooManyRequests
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))); // exponential back-off retry strategy;
            services.AddSingleton<IWebClientConfig>(sp => clients);
        }
    
        return services;
    }
}