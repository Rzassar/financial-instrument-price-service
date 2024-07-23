using Amega.UI.WebApi.Infrastructure.InstrumentProvider.Models;

namespace Amega.UI.WebApi.Infrastructure.WebSocketService
{
    public interface IWebSocketService
    {
        #region Events

        /// <summary>
        /// Fiers when a new message has received.
        /// </summary>
        event EventHandler<MessageReceivedArgs> MessageReceived;

        /// <summary>
        /// Fires when websocket gets connected or reconnected.
        /// </summary>
        event EventHandler<ConnectedArgs> Connected;

        #endregion

        #region Fields & Properties

        eAssetClassType AssetClassType { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Sends a message through the WebSocket.
        /// </summary>
        Task SendMessageAsync(string message, CancellationToken ct = default);

        /// <summary>
        /// Allowing the WebSocket to start listening to an endpoint.
        /// </summary>
        void StartListening();

        #endregion
    }
}