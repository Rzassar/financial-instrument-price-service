using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models.Quote;
using Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.QuoteParsingStrategy;
using Amega.UI.WebApi.Infrastructure.WebSocketService;
using Amega.UI.WebApi.Model.AmegaSettings;
using Amega.UI.WebApi.Services.InstrumentProvider;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ExtraSettings = Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider.TiingoInstrumentProviderExtraSettings;

namespace Amega.UI.WebApi.Infrastructure.InstrumentProvider.TiingoInstrumentProvider
{
    //NOTE: This provider maintains a single WebSocket connection to Tiingo,
    //      efficiently serving data to 1000+ subscribers without creating additional connections.
    //      Also, caching mechanism is implemented to reduce load on the external API.
    public sealed class TiingoInstrumentProvider : IInstrumentProvider, IDisposable
    {
        #region Events

        public event EventHandler<InstrumentProviderArgs> PriceChanged = delegate { };

        #endregion

        #region Fields & Properties

        private readonly ILogger<TiingoInstrumentProvider> logger;

        private readonly InstrumentProviderSettings settings;

        private readonly IDistributedCache cache;

        private readonly IEnumerable<IWebSocketService> webSocketServices;

        private readonly HttpClient httpClient;

        private int subscriptionId;

        private readonly Dictionary<eAssetClassType, WebSocketServiceEntry> webSockets;

        //NOTE: Enables polimorfic deserialization.
        private static readonly Newtonsoft.Json.JsonSerializerSettings jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
        {
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };

        public IEnumerable<eInstrument> AvailableInstruments { get; }

        public static string ProviderName => "Tiingo";

        public string GetProviderName()
            => ProviderName;

        #endregion

        #region Constructor

        public TiingoInstrumentProvider(ILogger<TiingoInstrumentProvider> logger,
                                        IOptions<AmegaSettings> settings,
                                        IDistributedCache cache,
                                        IEnumerable<IWebSocketService> webSocketServices)
        {
            httpClient = new HttpClient();

            this.logger = logger;
            this.cache = cache;
            this.webSocketServices = webSocketServices;

            //NOTE: Code AvailableInstruments = Enum.GetValues<eInstrument>();
            //      is not desirable as the items in eInstrument may be added occasionally
            //      while TiingoInstrumentProvider may not support them, resulting in runtime conflict.
            AvailableInstruments = new List<eInstrument>
            {
                eInstrument.EURUSD,
                eInstrument.USDJPY,
                eInstrument.BTCUSD
            };

            this.settings = settings
                                .Value
                                .InstrumentProviderSettingsList
                                .First(item => item.Name == ProviderName);

            webSockets = webSocketServices.Select(wss =>
            {
                wss.MessageReceived += HandleWebSocketMessage;
                wss.Connected += HandleWebSocketConnected;

                var assetType = wss.AssetClassType;
                var config = this.settings.AssetClasses.First(item => item.AssetClassType == assetType);
                var extraSettings = JsonSerializer.Deserialize<ExtraSettings>(config.Extended);
                var parserFactory = assetType switch
                {
                    eAssetClassType.Forex => (IQuoteParsingFactory)new ForexQuoteParsingFactory(),
                    eAssetClassType.Crypto => (IQuoteParsingFactory)new CryptoQuoteParsingFactory(),
                    _ => throw new NotImplementedException("")
                };

                wss.StartListening();

                return new
                {
                    Key = assetType,
                    Value = new WebSocketServiceEntry(wss, parserFactory, config, extraSettings)
                };
            })
            .ToDictionary(item => item.Key, item => item.Value);

            Task.Run(async () => await InitializeCache());
        }

        #endregion

        #region Methods

        public bool SupportInstrument(eInstrument instrument)
            => AvailableInstruments.Contains(instrument);

        public async Task<QuoteBase> GetPriceAsync(eInstrument instrument, CancellationToken ct)
        {
            var ticker = instrument.ToString().ToLower();
            var assetType = AssetClassInstrumetMapper.GetByInstrument(instrument);

            var value = await cache.GetStringAsync(ticker, ct);
            var dd = value is null ?
                        null :
                        assetType switch
                        {
                            eAssetClassType.Crypto => Newtonsoft.Json.JsonConvert.DeserializeObject<CryptoQuote>(value, jsonSettings),
                            eAssetClassType.Forex => Newtonsoft.Json.JsonConvert.DeserializeObject<ForexQuote>(value, jsonSettings),
                            eAssetClassType.Stock => Newtonsoft.Json.JsonConvert.DeserializeObject<QuoteBase>(value, jsonSettings),
                            _ => throw new InvalidOperationException()
                        };
            return dd;
        }

        private async Task InitializeCache()
        {
            foreach (var wss in webSockets)
            {
                var instruments = GetAvailableTickersByAssetType(wss.Key);
                var tickers = string.Join(',', instruments);
                var url = wss.Value
                            .ExtraSettings
                            .APIEndPoint
                            .Replace("{tickers}", tickers)
                            .Replace("{APIKey}", settings.APIKey);

                try
                {
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(content);

                    foreach (var item in data.EnumerateArray())
                    {
                        try
                        {
                            var quote = wss.Value.parserFactory.ParseFromRestApi(item);
                            await cache.SetStringAsync(quote.Ticker, JsonSerializer.Serialize(item));
                        }
                        catch (Exception exp)
                        {
                            logger.LogError(exp, "Failed to parse Tiingo API response.");
                            continue;
                        }
                    }
                }
                catch (HttpRequestException exp)
                {
                    logger.LogError(exp, "Failed to initialize cache from Tiingo API");
                    throw;
                }
            }
        }

        private async void HandleWebSocketMessage(object sender, MessageReceivedArgs e)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(e.Message);

                if (jsonElement.TryGetProperty("messageType", out var messageTypeElement))
                {
                    string messageType = messageTypeElement.GetString();

                    switch (messageType)
                    {
                        case "I":
                            subscriptionId = ParseInfoMessage(jsonElement);
                            break;
                        case "A":
                            var assetType = ((IWebSocketService)sender).AssetClassType;
                            var parser = webSockets[assetType].parserFactory;
                            var quote = parser.ParseFromWebSocket(jsonElement);
                            await UpdateCacheAndNotify(quote);
                            break;
                        case "H":
                            ParseHeartbeatMessage(jsonElement);
                            break;
                        case "E":
                            ParseAndHandleErrorMessage(jsonElement);
                            break;
                        default:
                            logger.LogError($"Unknown message type for message: {e.Message}");
                            break;
                    }
                }
                else
                {
                    logger.LogError("Message type not found in the message: {Message}", e.Message);
                }
            }
            catch (JsonException exp)
            {
                logger.LogError(exp, "Error parsing JSON: Message: {Message}", e.Message);
            }
            catch (Exception exp)
            {
                logger.LogError(exp, "Unexpected error processing message: {Message}", e.Message);
            }
        }

        private async void HandleWebSocketConnected(object sender, ConnectedArgs e)
        {
            var wss = (IWebSocketService)sender;
            var assetType = wss.AssetClassType;
            var tickers = GetAvailableTickersByAssetType(wss.AssetClassType);
            var thresholdLevel = webSockets[assetType].ExtraSettings.ThresholdLevel;

            await SubscribeTickers(wss, tickers, thresholdLevel);
        }

        private async Task UpdateCacheAndNotify(QuoteBase quote)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(quote, jsonSettings);
            await cache.SetStringAsync(quote.Ticker, json);
            OnPriceChanged(quote);
        }

        private void OnPriceChanged(QuoteBase quote)
        {
            var args = new InstrumentProviderArgs(quote);
            PriceChanged(this, args);
        }

        private async Task SubscribeTickers(IWebSocketService wss, IEnumerable<string> tickers, int thresholdLevel)
        {
            var subscribeMessage = new
            {
                eventName = "subscribe",
                authorization = settings.APIKey,
                eventData = new { thresholdLevel = thresholdLevel, tickers }
            };

            //TODO: Implement a retry pattern.
            try
            {
                await wss.SendMessageAsync
                (
                    Newtonsoft.Json.Linq.JObject.FromObject(subscribeMessage).ToString(),
                    default
                );
            }
            catch (Exception exp)
            {
                logger.LogCritical(exp, "The service {service} cannot subscribe tickers due to an error.", nameof(WebSocketService));
                //TODO: Raise an event for monitoring and state management purposes.
            }
        }

        IEnumerable<string> GetAvailableTickersByAssetType(eAssetClassType assetType)
        {
            return AvailableInstruments
                        .Where(item => AssetClassInstrumetMapper.GetByInstrument(item) == assetType)
                        .Select(item => item.ToString().ToLower());
        }

        #region Message parsers

        private int ParseInfoMessage(JsonElement jsonElement)
        {
            if (!jsonElement.TryGetProperty("response", out var responseElement))
                throw new InvalidOperationException();

            int code = responseElement.GetProperty("code").GetInt32();
            string responseMessage = responseElement.GetProperty("message").GetString();

            if (code != 200 || responseMessage != "Success")
            {
                //TODO: Take either of the following actions:
                //      Throw exception, retry to send, custom action based on the returned code.
                logger.LogInformation($"Subscription failed. Code: {code}, Message: {responseMessage}");
                throw new InvalidOperationException();
            }

            logger.LogInformation($"Subscription successful. SubscriptionId: {subscriptionId}");
            return jsonElement.GetProperty("data").GetProperty("subscriptionId").GetInt32();
        }

        private void ParseHeartbeatMessage(JsonElement jsonElement)
        {
            if (jsonElement.GetProperty("response").GetProperty("code").GetInt32() != 200)
                throw new InvalidOperationException();
        }

        private void ParseAndHandleErrorMessage(JsonElement jsonElement)
        {
            if (jsonElement.TryGetProperty("response", out var responseElement))
            {
                int code = responseElement.GetProperty("code").GetInt32();
                string errorMessage = responseElement.GetProperty("message").GetString();

                logger.LogError("Received error from WebSocket. Code: {Code}, Message: {ErrorMessage}", code, errorMessage);

                if (code == 400 && errorMessage.Contains("thresholdLevel not valid"))
                {
                    logger.LogCritical("Invalid thresholdLevel. Please check your configuration.");
                }

                //TODO: Add more specific error handling as needed.
            }
            else
            {
                logger.LogError("Error message format is unexpected: {JsonElement}", jsonElement.ToString());
            }

            // TODO: Depending on the error, implement retry logic,
            //      notify administrators, or take other corrective actions.
        }

        #endregion

        public void Dispose()
        {
            if (httpClient is not null)
                httpClient.Dispose();
        }

        #endregion
    }
}