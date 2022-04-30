using System;
using System.Net.Sockets;
using System.Threading;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.VoiceNext.VoiceGatewayEntities;
using DSharpPlus.VoiceNext.VoiceGatewayEntities.Payloads;

namespace DSharpPlus.VoiceNext
{
    public sealed partial class VoiceNextConnection : IDisposable
    {
        public DiscordClient Client { get; }
        public DiscordGuild Guild { get; }
        public DiscordChannel Channel { get; internal set; }
        public VoiceNextConfiguration Configuration { get; }
        public bool IsConnected = false;
        public CancellationToken CancellationToken => this._cancellationTokenSource.Token;

        internal CancellationTokenSource _cancellationTokenSource { get; set; } = new();
        internal DiscordVoiceStateUpdate _voiceStateUpdate { get; }
        internal DiscordVoiceServerUpdatePayload _voiceServerUpdate { get; set; }
        internal ConnectionEndpoint _webSocketEndpoint { get; set; }
        internal IWebSocketClient _voiceWebsocket { get; set; }
        internal bool _shouldResume = false;

        private bool _disposedValue;
        private DiscordVoiceReadyPayload? _voiceReadyPayload { get; set; }
        private DiscordVoiceHelloPayload? _voiceHelloPayload { get; set; }
        private string? _selectedProtocol => this._voiceReadyPayload?.Modes[0];
        private UdpClient? _udpClient { get; set; }
        private DiscordVoiceSessionDescriptionPayload? _voiceSessionDescriptionPayload { get; set; }

        internal VoiceNextConnection(DiscordClient client, DiscordChannel voiceChannel, VoiceNextConfiguration configuration, DiscordVoiceStateUpdate voiceStateUpdate, DiscordVoiceServerUpdatePayload voiceServerUpdatePayload)
        {
            this.Client = client;
            this.Guild = voiceChannel.Guild;
            this.Channel = voiceChannel;
            this.Configuration = configuration;

            // We're not supposed to cache these, however they're required when authenticating/resuming to a session in other methods. As such, don't use them otherwise.
            this._voiceStateUpdate = voiceStateUpdate;
            this._voiceServerUpdate = voiceServerUpdatePayload;

            // Setup endpoint
            if (this._voiceServerUpdate.Endpoint == null)
            {
                throw new InvalidOperationException($"The {nameof(this._voiceServerUpdate.Endpoint)} argument is null. A null endpoint means that the voice server allocated has gone away and is trying to be reallocated. You should attempt to disconnect from the currently connected voice server, and not attempt to reconnect until a new voice server is allocated.");
            }
            var endpointIndex = this._voiceServerUpdate.Endpoint.LastIndexOf(':');
            var endpointPort = 443;
            string? endpointHost;
            if (endpointIndex != -1) // Determines if the endpoint is a ip address or a hostname
            {
                endpointHost = this._voiceServerUpdate.Endpoint.Substring(0, endpointIndex);
                endpointPort = int.Parse(this._voiceServerUpdate.Endpoint.Substring(endpointIndex + 1));
            }
            else
            {
                endpointHost = this._voiceServerUpdate.Endpoint;
            }

            this._webSocketEndpoint = new ConnectionEndpoint
            {
                Hostname = endpointHost,
                Port = endpointPort
            };

            // Setup websocket
            this._voiceWebsocket = this.Client.Configuration.WebSocketClientFactory(this.Client.Configuration.Proxy);
            this._voiceWebsocket.Connected += this.WebsocketOpenedAsync;
            this._voiceWebsocket.Disconnected += this.WebsocketClosedAsync;
            this._voiceWebsocket.MessageReceived += this.WebsocketMessageAsync;
            this._voiceWebsocket.ExceptionThrown += this.WebsocketExceptionAsync;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    this._cancellationTokenSource.Cancel();
                    this._cancellationTokenSource.Dispose();
                    this._voiceWebsocket.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~VoiceNextConnection()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
