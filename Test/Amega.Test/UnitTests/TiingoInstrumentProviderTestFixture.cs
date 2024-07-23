using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Amega.Test.UnitTests
{
    public class TiingoInstrumentProviderTestFixture
    {
        #region Fields & Properties

        public Mock<ILogger<TiingoInstrumentProvider>> LoggerMock { get; }

        public Mock<IOptions<AmegaSettings>> SettingsMock { get; }

        public Mock<IDistributedCache> CacheMock { get; }

        public Mock<IWebSocketService> WebSocketServiceMock { get; }

        public IWebSocketService WebSocketService { get; }

        #endregion

        #region Constructor

        public TiingoInstrumentProviderTestFixture()
        {
            LoggerMock = new Mock<ILogger<TiingoInstrumentProvider>>();
            SettingsMock = new Mock<IOptions<AmegaSettings>>();

            CacheMock = new Mock<IDistributedCache>();
            CacheMock
                .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            WebSocketServiceMock = new Mock<IWebSocketService>();
            WebSocketService = WebSocketServiceMock.Object;
            WebSocketServiceMock.Setup(ws => ws.AssetClassType).Returns(eAssetClassType.Forex);

            // Setup mock settings
            var webSocketSettings = new InstrumentProviderSettings
            {
                Name = "Tiingo",
                APIKey = "test-api-key",
                AssetClasses = new List<AssetClassSettings>
                {
                    new AssetClassSettings
                    {
                        AssetClassType = eAssetClassType.Forex,
                        Extended = JsonSerializer.Serialize(new { ThresholdLevel = 5, APIEndPoint = "https://api.tiingo.com/tiingo" })
                    }
                }
            };
            SettingsMock.Setup(s => s.Value).Returns(new AmegaSettings
            {
                InstrumentProviderSettingsList = new List<InstrumentProviderSettings> { webSocketSettings }
            });

            WebSocketServiceMock.Setup(ws => ws.AssetClassType).Returns(eAssetClassType.Forex);
        }

        #endregion

        #region Methods

        public void ResetWebSocketServiceMock()
        {
            WebSocketServiceMock.Reset();
            WebSocketServiceMock.Setup(ws => ws.AssetClassType).Returns(eAssetClassType.Forex);
        }

        public TiingoInstrumentProvider CreateProvider()
        {
            return new TiingoInstrumentProvider
            (
                LoggerMock.Object,
                SettingsMock.Object,
                CacheMock.Object,
                new[] { WebSocketServiceMock.Object }
            );
        }

        #endregion
    }
}