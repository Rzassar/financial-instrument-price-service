using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Services.InstrumentProvider;

namespace Amega.UI.WebApi.Model.AmegaSettings
{
    /// <summary>
    /// Preserves settings related to each <see cref="IInstrumentProvider"/> 
    /// and the corresponding <see cref="IWebSocketService"/>.
    /// </summary>
    public sealed class InstrumentProviderSettings
    {
        public string Name { get; set; }

        public IEnumerable<AssetClassSettings> AssetClasses { get; set; }

        public string APIKey { get; set; }

        public int RetryCount { get; set; }
    }
}