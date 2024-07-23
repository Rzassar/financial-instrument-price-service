using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using System.Text.Json;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.QuoteParsingStrategy
{
    public class CryptoQuoteParsingFactory : IQuoteParsingFactory
    {
        public QuoteBase ParseFromRestApi(JsonElement json)
        {
            var topOfBookData = json.GetProperty("topOfBookData")[0];

            return new CryptoQuote
            {
                Ticker = json.GetProperty("ticker").GetString(),
                QuoteTimestamp = topOfBookData.GetProperty("quoteTimestamp").GetDateTime(),
                BaseCurrency = json.GetProperty("baseCurrency").GetString(),
                QuoteCurrency = json.GetProperty("quoteCurrency").GetString(),
                AskSize = topOfBookData.GetProperty("askSize").GetDecimal(),
                BidSize = topOfBookData.GetProperty("bidSize").GetDecimal(),
                LastSaleTimestamp = topOfBookData.GetProperty("lastSaleTimestamp").GetDateTime(),
                LastPrice = topOfBookData.GetProperty("lastPrice").GetDecimal(),
                AskPrice = topOfBookData.GetProperty("askPrice").GetDecimal(),
                BidExchange = topOfBookData.GetProperty("bidExchange").GetString(),
                LastSizeNotional = topOfBookData.GetProperty("lastSizeNotional").GetDecimal(),
                LastExchange = topOfBookData.GetProperty("lastExchange").GetString(),
                AskExchange = topOfBookData.GetProperty("askExchange").GetString(),
                BidPrice = topOfBookData.GetProperty("bidPrice").GetDecimal(),
                LastSize = topOfBookData.GetProperty("lastSize").GetDecimal()
            };
        }

        public QuoteBase ParseFromWebSocket(JsonElement json)
        {
            var data = json.GetProperty("data");

            return new CryptoQuote
            {
                Ticker = data[1].GetString(),
                QuoteTimestamp = DateTime.Parse(data[2].GetString()),
                LastExchange = data[3].GetString(),
                LastSize = data[4].GetDecimal(),
                LastPrice = data[5].GetDecimal()
            };
        }
    }
}