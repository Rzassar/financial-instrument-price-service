using Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Amega.UI.WebApi.Services.InstrumentProvider
{
    public static class IInstrumentProviderExtensions
    {
        public static IServiceCollection AddInstrumentProvider<T>(this IServiceCollection services, Func<IOptions<AmegaSettings>, IDistributedCache, IEnumerable<IWebSocketService>, IServiceProvider, T> createProviderFunc)
            where T : class, IInstrumentProvider
        {
            services.AddSingleton<IInstrumentProvider, T>(serviceProvider =>
            {
                Type type = typeof(T);
                var propertyInfo = type.GetProperty("ProviderName", BindingFlags.Static | BindingFlags.Public);
                var providerName = (string)propertyInfo.GetValue(null);

                var settings = serviceProvider.GetRequiredService<IOptions<AmegaSettings>>();
                var cache = serviceProvider.GetRequiredService<IDistributedCache>();

                //NOTE: Find the corresponding WebSocketServices for an InstrumentProvider.
                var instrumentProviderSettings = settings
                                                    .Value
                                                    .InstrumentProviderSettingsList
                                                    .First(item => item.Name == providerName);
                var webSocketServices = new List<IWebSocketService>();
                foreach (var assetClass in instrumentProviderSettings.AssetClasses)
                {
                    var webSocketInstanceName = $"{providerName}-{assetClass.AssetClassType}";
                    webSocketServices.Add
                    (
                        serviceProvider.GetRequiredKeyedService<IWebSocketService>(webSocketInstanceName)
                    );
                }

                return createProviderFunc(settings, cache, webSocketServices, serviceProvider);
            });

            return services;
        }

        public static IServiceCollection AddTiingoInstrumentProvider(this IServiceCollection services)
        {
            services.AddInstrumentProvider
            (
                (settings, cache, webSocketServices, serviceProvider) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<TiingoInstrumentProvider>>();
                    return new TiingoInstrumentProvider(logger, settings, cache, webSocketServices);
                }
            );

            return services;
        }
    }
}