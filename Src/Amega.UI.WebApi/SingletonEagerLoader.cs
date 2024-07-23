using Amega.UI.WebApi.Hubs;
using Amega.UI.WebApi.Services.InstrumentProvider;

namespace Amega.UI.WebApi
{
    /// <summary>
    /// By default, singleton services inside the DI container are lazy loaded.
    /// This class is merely used to eager load singleton scoped services
    /// such as <see cref="IInstrumentProvider"/> or <see cref="PriceUpdateService"/>.
    /// </summary>
    public sealed class SingletonEagerLoader : IHostedService
    {
        private readonly PriceUpdateService updateService;

        public SingletonEagerLoader(PriceUpdateService updateService)
        {
            this.updateService = updateService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}