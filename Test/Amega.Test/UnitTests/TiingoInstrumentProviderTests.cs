using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Newtonsoft.Json.Linq;

namespace Amega.Test.UnitTests
{
    public class TiingoInstrumentProviderTests : IClassFixture<TiingoInstrumentProviderTestFixture>
    {
        #region Fields & Properties

        private readonly TiingoInstrumentProviderTestFixture fixture;

        #endregion

        #region Constructor

        public TiingoInstrumentProviderTests(TiingoInstrumentProviderTestFixture fixture)
        {
            this.fixture = fixture;
            this.fixture.ResetWebSocketServiceMock();
        }

        #endregion

        #region Test Cases

        [Fact]
        public void TiingoInstrumentProvider_MessageTypeA_ParsesAndFiresEvent()
        {
            // Arrange
            var provider = fixture.CreateProvider();

            // Setup event tracking
            var eventFired = false;
            QuoteBase eventQuote = null;

            provider.PriceChanged += (sender, args) =>
            {
                eventFired = true;
                eventQuote = args.Quote;
            };

            // Act
            var tradeMessage = @"{
        ""messageType"": ""A"",
        ""data"": [
            ""forex"",
            ""eurusd"",
            ""2024-06-22T10:00:00Z"",
            100000,
            1.1234,
            1.12345,
            1.1235,
            100000
        ]
    }";

            fixture.WebSocketServiceMock.Raise(
                ws => ws.MessageReceived += null,
                fixture.WebSocketService,
                new MessageReceivedArgs(tradeMessage)
            );

            // Assert
            Assert.True(eventFired);
            Assert.NotNull(eventQuote);
            Assert.IsType<ForexQuote>(eventQuote);
            var forexQuote = (ForexQuote)eventQuote;
            Assert.Equal("eurusd", forexQuote.Ticker);
            Assert.Equal(DateTime.Parse("2024-06-22T10:00:00Z"), forexQuote.QuoteTimestamp);
            Assert.Equal(100000m, forexQuote.BidSize);
            Assert.Equal(1.1234m, forexQuote.BidPrice);
            Assert.Equal(1.12345m, forexQuote.MidPrice);
            Assert.Equal(1.1235m, forexQuote.AskPrice);
            Assert.Equal(100000m, forexQuote.AskSize);

            // Verify that the cache was updated
            fixture.CacheMock.Verify(
                c => c.SetAsync(
                    "eurusd",
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public void TiingoInstrumentProvider_Connected_RegistersForAvailableInstruments()
        {
            // Arrange
            var provider = fixture.CreateProvider();

            string capturedMessage = null;
            fixture.WebSocketServiceMock
                .Setup(ws => ws.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((message, token) => capturedMessage = message)
                .Returns(Task.CompletedTask);

            // Act
            fixture.WebSocketServiceMock.Raise
            (
                ws => ws.Connected += null,
                fixture.WebSocketService,
                new ConnectedArgs(1)
            );

            // Assert
            Assert.NotNull(capturedMessage);
            var subscribeMessage = JObject.Parse(capturedMessage);

            Assert.Equal("subscribe", subscribeMessage["eventName"].ToString());
            Assert.Equal("test-api-key", subscribeMessage["authorization"].ToString());

            var eventData = subscribeMessage["eventData"];
            Assert.Equal(5, eventData["thresholdLevel"].Value<int>());

            var tickers = eventData["tickers"].ToObject<List<string>>();
            Assert.Contains("eurusd", tickers);
            Assert.Contains("usdjpy", tickers);
            Assert.DoesNotContain("btcusd", tickers);  //NOTE: BTCUSD is not in Forex asset class.
            Assert.Equal(2, tickers.Count);  //NOTE: Only Forex instruments should be included.

            fixture.WebSocketServiceMock.Verify
            (
                ws => ws.SendMessageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        #endregion
    }
}