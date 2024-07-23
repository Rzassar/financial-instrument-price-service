using Amega.UI.WebApi.Services.InstrumentProvider;
using Microsoft.AspNetCore.SignalR;

namespace Amega.UI.WebApi.Hubs
{
    public sealed class TickerHub : Hub<ITickerHubClient>
    {
        #region Fields & Properties

        private readonly ILogger<Hub<ITickerHubClient>> logger;

        private readonly PriceUpdateService priceUpdateService;

        #endregion

        #region Constructor

        public TickerHub(ILogger<Hub<ITickerHubClient>> logger, PriceUpdateService priceUpdateService)
        {
            this.logger = logger;
            this.priceUpdateService = priceUpdateService;
        }

        #endregion

        #region Methods

        //NOTE: This hub is designed to efficiently handle 1000+ concurrent WebSocket connections.
        //      SignalR's built-in connection management and scaling capabilities are utilized here.
        public async Task Subscribe(string instrument)
        {
            if (priceUpdateService.AvailableInstruments.Contains(instrument))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, instrument);
                logger.LogTrace("Client {ConnectionId} subscribed to {Instrument}", Context.ConnectionId, instrument);
            }
            else
            {
                logger.LogWarning("Client {ConnectionId} attempted to subscribe to unavailable instrument: {Instrument}", Context.ConnectionId, instrument);
                throw new HubException($"Instrument '{instrument}' is not available.");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, instrument);
        }

        public async Task Unsubscribe(string instrument)
        {
            if (priceUpdateService.AvailableInstruments.Contains(instrument, StringComparer.OrdinalIgnoreCase))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, instrument);
                logger.LogTrace("Client {ConnectionId} unsubscribed from {Instrument}", Context.ConnectionId, instrument);
            }
            else
            {
                logger.LogWarning("Client {ConnectionId} attempted to unsubscribe from unavailable instrument: {Instrument}", Context.ConnectionId, instrument);
                throw new HubException($"Instrument '{instrument}' is not available.");
            }
        }

        #endregion
    }
}