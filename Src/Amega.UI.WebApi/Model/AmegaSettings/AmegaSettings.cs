using Amega.UI.WebApi.Services.InstrumentProvider;

namespace Amega.UI.WebApi.Model.AmegaSettings
{
    public sealed class AmegaSettings
    {
        public IEnumerable<InstrumentProviderSettings> InstrumentProviderSettingsList { get; set; }
    }
}