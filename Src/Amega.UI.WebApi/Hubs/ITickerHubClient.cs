using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;

namespace Amega.UI.WebApi.Hubs
{
    public interface ITickerHubClient
    {
        Task UpdateTicker(string provider, QuoteBase quote);
    }
}