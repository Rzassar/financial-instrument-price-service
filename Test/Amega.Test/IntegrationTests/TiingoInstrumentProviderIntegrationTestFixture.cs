using Amega.UI.WebApi.Hubs;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace Amega.Test.IntegrationTests
{
    public class TiingoInstrumentProviderIntegrationTestFixture : IDisposable
    {
        #region Fields & Properties

        public Mock<ILogger<TiingoInstrumentProvider>> LoggerMock { get; }

        public Mock<IOptions<AmegaSettings>> SettingsMock { get; }

        public Mock<IDistributedCache> CacheMock { get; }

        public Mock<IWebSocketService> WebSocketServiceMock { get; }

        public TiingoInstrumentProvider TiingoProvider { get; }

        public Mock<IHubContext<TickerHub, ITickerHubClient>> HubContextMock { get; }

        public Mock<IHubClients<ITickerHubClient>> HubClientsMock { get; }

        public Mock<ITickerHubClient> GroupClientMock { get; }

        #endregion

        #region Constructor

        public TiingoInstrumentProviderIntegrationTestFixture()
        {
            LoggerMock = new Mock<ILogger<TiingoInstrumentProvider>>();
            SettingsMock = new Mock<IOptions<AmegaSettings>>();
            CacheMock = new Mock<IDistributedCache>();
            WebSocketServiceMock = new Mock<IWebSocketService>();
            HubContextMock = new Mock<IHubContext<TickerHub, ITickerHubClient>>();
            HubClientsMock = new Mock<IHubClients<ITickerHubClient>>();
            GroupClientMock = new Mock<ITickerHubClient>();

            SetupMocks();

            TiingoProvider = new TiingoInstrumentProvider
            (
                LoggerMock.Object,
                SettingsMock.Object,
                CacheMock.Object,
                new[] { WebSocketServiceMock.Object }
            );
        }

        #endregion

        #region Methods

        private void SetupMocks()
        {
            var instrumentProviderSettings = new InstrumentProviderSettings
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
                InstrumentProviderSettingsList = new List<InstrumentProviderSettings> { instrumentProviderSettings }
            });

            WebSocketServiceMock.Setup(ws => ws.AssetClassType).Returns(eAssetClassType.Forex);

            HubContextMock.Setup(h => h.Clients).Returns(HubClientsMock.Object);
            HubClientsMock.Setup(c => c.Group("eurusd")).Returns(GroupClientMock.Object);
        }

        public void Dispose()
        {
            //NOTE: Perform any cleanup if necessary.
        }

        #endregion
    }
}