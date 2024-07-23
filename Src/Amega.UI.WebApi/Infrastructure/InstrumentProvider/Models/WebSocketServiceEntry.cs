using Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.QuoteParsingStrategy;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using ExtraSettings = Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.TiingoInstrumentProviderExtraSettings;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models
{
    public sealed record WebSocketServiceEntry
    (
        IWebSocketService WebSocketService,

        IQuoteParsingFactory parserFactory,

        AssetClassSettings Settings,

        ExtraSettings ExtraSettings
    );
}