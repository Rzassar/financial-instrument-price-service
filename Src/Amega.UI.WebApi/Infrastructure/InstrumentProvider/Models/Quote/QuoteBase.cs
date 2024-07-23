namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote
{
    public abstract class QuoteBase
    {
        public string Ticker { get; set; }

        public DateTime QuoteTimestamp { get; set; }

        public decimal BidPrice { get; set; }

        public decimal BidSize { get; set; }

        public decimal AskPrice { get; set; }

        public decimal AskSize { get; set; }
    }
}