using Amega.UI.WebApi.Hubs;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using Amega.UI.WebApi.Services.InstrumentProvider;
using Microsoft.AspNetCore.Mvc;

namespace Amega.UI.WebApi.Controllers
{
    public class TickerController : Controller
    {
        #region Fields & Properties

        private readonly IEnumerable<IInstrumentProvider> providers;

        private readonly PriceUpdateService priceUpdateService;

        #endregion

        #region Constructor

        public TickerController(IEnumerable<IInstrumentProvider> providers, PriceUpdateService priceUpdateService)
        {
            this.providers = providers;
            this.priceUpdateService = priceUpdateService;
        }

        #endregion

        [HttpGet("Get")]
        public IActionResult Get()
        {
            return Ok(priceUpdateService.AvailableInstruments);
        }

        [HttpGet("GetPrice")]
        public async Task<IActionResult> GetPrice(string ticker, CancellationToken ct)
        {
            eInstrument instrument;
            QuoteBase price = null;

            try
            {
                instrument = Enum.Parse<eInstrument>(ticker, true);
            }
            catch
            {
                return BadRequest("Invalid ticker.");
            }

            var provider = providers.FirstOrDefault(provider => provider.SupportInstrument(instrument));
            if (provider is not null)
                price = await provider.GetPriceAsync(instrument, ct);

            return price is null ?
                            BadRequest($"Price {ticker} is not available.") :
                            Ok(price);
        }
    }
}