using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;

namespace Amega.UI.WebApi.Services.InstrumentProvider
{
    /// <summary>
    /// Provides the ability to inquire about financial instruments in real-time or on demand.
    /// </summary>
    public interface IInstrumentProvider
    {
        #region Events

        /// <summary>
        /// Fires when a financial instrument changes.
        /// </summary>
        event EventHandler<InstrumentProviderArgs> PriceChanged;

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Returns the name of the provider.
        /// </summary>
        static string ProviderName { get; }

        /// <summary>
        /// Enables access to the <see cref="ProviderName"/> via an instace object.
        /// </summary>
        /// <returns></returns>
        virtual string GetProviderName()
            => ProviderName;

        /// <summary>
        /// Gets all available finantial instruments.
        /// </summary>
        IEnumerable<eInstrument> AvailableInstruments { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Checks whether the current <see cref="IInstrumentProvider"/> 
        /// supports a specific <see cref="eInstrument"/> or not.
        /// </summary>
        bool SupportInstrument(eInstrument instrument);

        /// <summary>
        /// Gets the price for the specific financial instrument.
        /// </summary>
        Task<QuoteBase> GetPriceAsync(eInstrument instrument, CancellationToken ct);

        #endregion
    }
}