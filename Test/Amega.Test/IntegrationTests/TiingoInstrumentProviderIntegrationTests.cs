using Amega.UI.WebApi.Controllers;
using Amega.UI.WebApi.Hubs;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Amega.Test.IntegrationTests
{
    public class TiingoInstrumentProviderIntegrationTests : IClassFixture<TiingoInstrumentProviderIntegrationTestFixture>
    {
        #region Fields & Properties

        private readonly TiingoInstrumentProviderIntegrationTestFixture fixture;

        #endregion

        #region Constructor

        public TiingoInstrumentProviderIntegrationTests(TiingoInstrumentProviderIntegrationTestFixture fixture)
        {
            this.fixture = fixture;
        }

        #endregion

        #region Test cases

        [Fact]
        public void TiingoInstrumentProvider_PriceChange_UpdatesSignalRClients()
        {
            // Arrange
            var priceUpdateServiceLogger = new Mock<ILogger<PriceUpdateService>>();
            var priceUpdateService = new PriceUpdateService(
                fixture.HubContextMock.Object,
                new[] { fixture.TiingoProvider },
                priceUpdateServiceLogger.Object
            );

            bool priceChangedEventRaised = false;
            fixture.TiingoProvider.PriceChanged += (sender, args) =>
            {
                priceChangedEventRaised = true;
            };

            // Act
            var priceChangeMessage = @"{
                ""messageType"": ""A"",
                ""data"": [
                    ""forex"",
                    ""eurusd"",
                    ""2024-07-22T10:00:00Z"",
                    100000,
                    1.1234,
                    1.12345,
                    1.1235,
                    100000
                ]
            }";
            fixture.WebSocketServiceMock.Raise(ws => ws.MessageReceived += null, fixture.WebSocketServiceMock.Object, new MessageReceivedArgs(priceChangeMessage));

            // Assert
            Assert.True(priceChangedEventRaised, "PriceChanged event was not raised");

            fixture.GroupClientMock.Verify
            (
                client => client.UpdateTicker
                (
                    "Tiingo",
                    It.Is<QuoteBase>(q => q is ForexQuote &&
                                          ((ForexQuote)q).Ticker == "eurusd" &&
                                          ((ForexQuote)q).BidPrice == 1.1234m &&
                                          ((ForexQuote)q).QuoteTimestamp == DateTime.Parse("2024-07-22T10:00:00Z"))
                ),
                Times.Once,
                "UpdateTicker was not called with the expected parameters"
            );
        }

        [Fact]
        public async Task TickerController_GetPrice_ReturnsCorrectPrice()
        {
            // Arrange
            var cachedQuote = new ForexQuote
            {
                Ticker = "eurusd",
                BidPrice = 1.1234m,
                AskPrice = 1.1235m,
                QuoteTimestamp = DateTime.Parse("2024-07-22T10:00:00Z")
            };
            var serializedQuote = JsonSerializer.Serialize(cachedQuote);
            var serializedQuoteBytes = System.Text.Encoding.UTF8.GetBytes(serializedQuote);

            fixture.CacheMock.Setup(c => c.GetAsync("eurusd", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(serializedQuoteBytes);

            var priceUpdateServiceLogger = new Mock<ILogger<PriceUpdateService>>();
            var priceUpdateService = new PriceUpdateService
            (
                fixture.HubContextMock.Object,
                new[] { fixture.TiingoProvider },
                priceUpdateServiceLogger.Object
            );

            var controller = new TickerController(new[] { fixture.TiingoProvider }, priceUpdateService);

            // Act
            var result = await controller.GetPrice("EURUSD", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<ForexQuote>(okResult.Value);
            Assert.Equal("eurusd", returnValue.Ticker);
            Assert.Equal(1.1234m, returnValue.BidPrice);
        }

        #endregion
    }
}