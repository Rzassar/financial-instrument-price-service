using System.Runtime.CompilerServices;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models
{
    public static class AssetClassInstrumetMapper
    {
        public static readonly Dictionary<eInstrument, eAssetClassType> Mapper = new()
        {
            {eInstrument.EURUSD, eAssetClassType.Forex },
            {eInstrument.USDJPY, eAssetClassType.Forex },
            {eInstrument.BTCUSD, eAssetClassType.Crypto }
        };

        public static eAssetClassType GetByInstrument(eInstrument instrument)
            => Mapper[instrument];
    }
}