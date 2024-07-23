using Amega.UI.WebApi.Services.InstrumentProvider;
using Microsoft.AspNetCore.SignalR;

namespace Amega.UI.WebApi.Hubs
{
    public class PriceUpdateService
    {
        #region Fields & Properties

        private readonly IHubContext<TickerHub, ITickerHubClient> hubContext;

        private readonly IEnumerable<IInstrumentProvider> providers;

        private readonly ILogger<PriceUpdateService> logger;

        private List<string> availableInstruments = new();

        public IReadOnlyList<string> AvailableInstruments
            => availableInstruments.AsReadOnly();

        #endregion

        #region Constructor

        public PriceUpdateService(IHubContext<TickerHub, ITickerHubClient> hubContext,
                                    IEnumerable<IInstrumentProvider> providers,
                                    ILogger<PriceUpdateService> logger)
        {
            this.hubContext = hubContext;
            this.providers = providers;
            this.logger = logger;

            availableInstruments = providers
                            .SelectMany(provider => provider.AvailableInstruments)
                            .Distinct()
                            .Select(item => item.ToString().ToLower())
                            .ToList();

            foreach (var provider in providers)
            {
                provider.PriceChanged += ProviderPriceChanged;
            }
        }

        #endregion

        #region Methods

        //NOTE: This service is optimized to handle price updates for 1000+ subscribers efficiently.
        //      It maintains a single connection to the data provider while broadcasting to multiple clients.
        private async void ProviderPriceChanged(object sender, InstrumentProviderArgs e)
        {
            var ticker = e.Quote.Ticker.ToLower();
            var provider = ((IInstrumentProvider)sender).GetProviderName();

            try
            {
                await hubContext.Clients.Group(ticker).UpdateTicker(provider, e.Quote);
                logger.LogTrace("Price update sent for {Ticker} from {Provider}", ticker, provider);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending price update for {Ticker} from {Provider}", ticker, provider);
            }
        }

        public void Dispose()
        {
            foreach (var provider in providers)
            {
                provider.PriceChanged -= ProviderPriceChanged;
            }
        }

        #endregion
    }
}