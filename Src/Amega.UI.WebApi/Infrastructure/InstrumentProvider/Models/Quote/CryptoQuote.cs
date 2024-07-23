namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote
{
    public class CryptoQuote : QuoteBase
    {
        public string BaseCurrency { get; set; }

        public string QuoteCurrency { get; set; }

        public DateTime LastSaleTimestamp { get; set; }

        public decimal LastPrice { get; set; }

        public string BidExchange { get; set; }

        public decimal LastSizeNotional { get; set; }

        public string LastExchange { get; set; }

        public string AskExchange { get; set; }

        public decimal LastSize { get; set; }
    }
}