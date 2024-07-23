using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using System.Text.Json;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.QuoteParsingStrategy
{
    public class ForexQuoteParsingFactory : IQuoteParsingFactory
    {
        public QuoteBase ParseFromRestApi(JsonElement json)
        {
            return new ForexQuote
            {
                Ticker = json.GetProperty("ticker").GetString(),
                QuoteTimestamp = json.GetProperty("quoteTimestamp").GetDateTime(),
                BidPrice = json.GetProperty("bidPrice").GetDecimal(),
                BidSize = json.GetProperty("bidSize").GetDecimal(),
                AskPrice = json.GetProperty("askPrice").GetDecimal(),
                AskSize = json.GetProperty("askSize").GetDecimal(),
                MidPrice = json.GetProperty("midPrice").GetDecimal()
            };
        }

        public QuoteBase ParseFromWebSocket(JsonElement json)
        {
            var data = json.GetProperty("data");
            return new ForexQuote
            {
                Ticker = data[1].GetString(),
                QuoteTimestamp = DateTime.Parse(data[2].GetString()),
                BidSize = data[3].GetDecimal(),
                BidPrice = data[4].GetDecimal(),
                MidPrice = data[5].GetDecimal(),
                AskPrice = data[6].GetDecimal(),
                AskSize = data[7].GetDecimal()
            };
        }
    }
}