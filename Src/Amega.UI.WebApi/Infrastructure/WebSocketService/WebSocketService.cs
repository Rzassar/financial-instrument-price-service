using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;
using Amega.UI.WebApi.Infrastructure.WebSocketService.MarkerInterfaces;
using Polly.Retry;
using System.Net.WebSockets;
using System.Text;

namespace Amega.UI.WebApi.Infrastructure.WebSocketService
{
    public class WebSocketService : BackgroundService, IWebSocketService, ICryptoWebSocketServiceMarkerInterface, IForexWebSocketServiceMarkerInterface, IStockWebSocketServiceMarkerInterface
    {
        #region Events

        public event EventHandler<MessageReceivedArgs> MessageReceived = delegate { };

        public event EventHandler<ConnectedArgs> Connected = delegate { };

        #endregion

        #region Fields & Properties

        private int retryCount = 0;

        private ClientWebSocket webSocket;

        private readonly ILogger<WebSocketService> logger;

        private readonly string url;

        private readonly AsyncRetryPolicy retryPolicy;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private readonly SemaphoreSlim instrumentProviderLock = new SemaphoreSlim(0, 1);

        public eAssetClassType AssetClassType { get; }

        #endregion

        #region Constructor

        public WebSocketService(ILogger<WebSocketService> logger,
                                eAssetClassType assetClassType,
                                string url,
                                AsyncRetryPolicy retryPolicy)
        {
            webSocket = new ClientWebSocket();

            this.logger = logger;
            AssetClassType = assetClassType;
            this.url = url;
            this.retryPolicy = retryPolicy;
        }

        #endregion

        #region Methods

        //TODO: Reduce the number of try/catch/finally blocks in favor of performance.
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                //NOTE: Wait untill the corresponding IInstrumentProvider executes.
                await instrumentProviderLock.WaitAsync();

                while (!ct.IsCancellationRequested)
                {
                    await retryPolicy.ExecuteAsync(() => ConnectAsync(ct));

                    while (webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
                    {
                        try
                        {
                            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                            if (result.MessageType == WebSocketMessageType.Text)
                            {
                                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                MessageReceived(this, new(message));

                                logger.LogTrace("message received from {url}: {message}", url, message);
                            }
                        }
                        catch (Exception exp) when (exp is not OperationCanceledException)
                        {
                            logger.LogError(exp, "Error in WebSocket background service.");
                            break;
                        }
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        await retryPolicy.ExecuteAsync(() => HandleReconnectAsync(ct));
                    }
                }
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                logger.LogCritical(exp, "The service {service} cannot continue due to an error.", nameof(WebSocketService));
                //TODO: Raise an event for monitoring and state management purposes.
            }
            finally
            {
                if (webSocket != null)
                {
                    if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    webSocket.Dispose();
                }

                logger.LogInformation("WebSocket execution finished: {url}", url);
            }
        }

        public void StartListening()
        {
            instrumentProviderLock.Release();
        }

        private async Task ConnectAsync(CancellationToken ct)
        {
            try
            {
                retryCount++;

                await webSocket.ConnectAsync(new Uri(url), ct);
                Connected(this, new(retryCount));

                logger.LogInformation($"WebSocket connected: {url}");
            }
            catch (Exception exp) when (exp is not OperationCanceledException)
            {
                logger.LogError(exp, "Error connecting WebSocket.");
                //TODO: Raise an event for monitoring and state management purposes.

                throw;
            }
        }

        private async Task HandleReconnectAsync(CancellationToken ct)
        {
            logger.LogInformation("Attempting to reconnect WebSocket...");

            if (webSocket.State is WebSocketState.Aborted or WebSocketState.Closed or WebSocketState.CloseReceived)
            {
                webSocket.Dispose();
                webSocket = new ClientWebSocket();
            }

            await ConnectAsync(ct);
        }

        public async Task SendMessageAsync(string message, CancellationToken ct = default)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);

                await semaphore.WaitAsync(ct);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, ct);

                logger.LogTrace("Message sent to {url}: {message}", url, message);
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion
    }
}