using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using System.Text.Json;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.QuoteParsingStrategy
{
    public interface IQuoteParsingFactory
    {
        QuoteBase ParseFromRestApi(JsonElement json);

        QuoteBase ParseFromWebSocket(JsonElement json);
    }
}