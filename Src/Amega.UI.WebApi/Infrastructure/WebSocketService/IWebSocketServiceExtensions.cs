using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.WebSocketService.MarkerInterfaces;
using Amega.UI.WebApi.Model.AmegaSettings;
using Polly;
using System.Net.WebSockets;

namespace Amega.UI.WebApi.Infrastructure.WebSocketService
{
    public static class IWebSocketServiceExtensions
    {
        public static IServiceCollection AddWebSocketServices(this IServiceCollection services, InstrumentProviderSettings settings)
        {
            //NOTE: Initialize websocketService for each AssetClass in the corresponding instrument provider settings
            foreach (var assetClassSettings in settings.AssetClasses)
            {
                var webSocketInstanceName = $"{settings.Name}-{assetClassSettings.AssetClassType}";

                services.AddKeyedSingleton<IWebSocketService, WebSocketService>(webSocketInstanceName, (serviceProvider, obj) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<WebSocketService>>();
                    var policy = Policy
                                    .Handle<WebSocketException>()
                                    .WaitAndRetryAsync(
                                        retryCount: settings.RetryCount,
                                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                        onRetry: (exception, timeSpan, retryCount, context) =>
                                        {
                                            logger.LogInformation($"Retry {retryCount} due to {exception}. Waiting {timeSpan} before next retry.");
                                        });

                    return new WebSocketService(logger, assetClassSettings.AssetClassType, assetClassSettings.Url, policy);
                });

                //NOTE: Enable the WebSocket background worker.
                switch (assetClassSettings.AssetClassType)
                {
                    case eAssetClassType.Crypto:
                        services.AddHostedService<ICryptoWebSocketServiceMarkerInterface>(sp =>
                            (ICryptoWebSocketServiceMarkerInterface)sp.GetRequiredKeyedService<IWebSocketService>(webSocketInstanceName));
                        break;
                    case eAssetClassType.Forex:
                        services.AddHostedService<IForexWebSocketServiceMarkerInterface>(sp =>
                            (IForexWebSocketServiceMarkerInterface)sp.GetRequiredKeyedService<IWebSocketService>(webSocketInstanceName));
                        break;
                    case eAssetClassType.Stock:
                        services.AddHostedService<IStockWebSocketServiceMarkerInterface>(sp =>
                            (IStockWebSocketServiceMarkerInterface)sp.GetRequiredKeyedService<IWebSocketService>(webSocketInstanceName));
                        break;
                    default:
                        throw new ArgumentException($"Unsupported asset class type: {assetClassSettings.AssetClassType}");
                }
            }

            return services;
        }
    }
}