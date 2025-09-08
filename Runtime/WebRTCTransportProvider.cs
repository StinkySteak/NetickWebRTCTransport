using Netick.Transport.WebRTC;
using Netick.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netick.Transport
{
    [CreateAssetMenu(fileName = nameof(WebRTCTransportProvider), menuName = "Netick/Transport/WebRTCTransportProvider")]
    public class WebRTCTransportProvider : NetworkTransportProvider
    {
        public UserRTCConfig RTCConfig;
        public SignalingServerConnectConfig SignalingServerConfig;

        private void Reset()
        {
            RTCConfig = new UserRTCConfig()
            {
                RTCTimeoutDuration = 3
            };

            RTCConfig.IceServers = new IceServer[1];
            RTCConfig.IceServers[0] = new IceServer()
            {
                Urls = new string[] { "stun:stun.l.google.com:19302" },
            };

            RTCConfig.IceCandidateGatheringConfig = new IceCandidateGatheringConfig()
            {
                GatherDuration = 0.5f,
                WaitGatheringToComplete = false,
            };
        }

        public override NetworkTransport MakeTransportInstance()
        {
            WebRTCTransport transport = new WebRTCTransport();
            transport.InitConfig(SignalingServerConfig, RTCConfig);

            return transport;
        }
    }

    public class WebRTCConnection : TransportConnection
    {
        public BaseWebRTCPeer Peer;

        public override IEndPoint EndPoint => Peer.EndPoint;

        public override int Mtu => 1200;

        public override void Send(IntPtr ptr, int length)
        {
            Peer.Send(ptr, length, isReliable: false);
        }
    }

    public unsafe class WebRTCTransport : NetworkTransport, IWebRTCNetEventListener
    {
        private Dictionary<BaseWebRTCPeer, WebRTCConnection> _connections;
        private Queue<WebRTCConnection> _freeClients;

        private BitBuffer _bitBuffer;
        private WebRTCNetManager _netManager;
        private SignalingServerConnectConfig _signalingServerConnectConfig;
        private UserRTCConfig _userRTCConfig;

        public HostAllocationService HostAllocationService => _netManager.HostAllocationService;

        public override void Init()
        {
            _netManager = new WebRTCNetManager();
            _bitBuffer = new BitBuffer(createChunks: false);

            _connections = new(Engine.Config.MaxPlayers);
            _freeClients = new(Engine.Config.MaxPlayers);

            for (int i = 0; i < Engine.Config.MaxPlayers; i++)
                _freeClients.Enqueue(new WebRTCConnection());

            _netManager.Init(Engine, this, _signalingServerConnectConfig, _userRTCConfig);
        }

        public void InitConfig(SignalingServerConnectConfig signalingServerConnectConfig, UserRTCConfig userRTCConfig)
        {
            _signalingServerConnectConfig = signalingServerConnectConfig;
            _userRTCConfig = userRTCConfig;
        }

        public override void Connect(string address, int port, byte[] connectionData, int connectionDataLength)
        {
            _netManager.Connect(address);
        }

        public override void Disconnect(TransportConnection connection)
        {
            WebRTCConnection webRTCConnection = (WebRTCConnection)connection;

            _netManager.DisconnectPeer(webRTCConnection.Peer);
        }

        public override void PollEvents()
        {
            _netManager.PollUpdate();
        }

        public override void Run(RunMode mode, int port)
        {
            _netManager.Start();
        }

        public override void Shutdown()
        {
            _netManager.Stop();
        }

        void IWebRTCNetEventListener.OnPeerConnected(BaseWebRTCPeer peer)
        {
            WebRTCConnection connection = _freeClients.Dequeue();
            connection.Peer = peer;

            _connections.Add(peer, connection);
            NetworkPeer.OnConnected(connection);
        }

        void IWebRTCNetEventListener.OnPeerDisconnected(BaseWebRTCPeer peer, DisconnectReason disconnectReason)
        {
            if (Engine.IsClient)
            {
                if (disconnectReason == DisconnectReason.SignalingServerUnreachable || disconnectReason == DisconnectReason.Timeout)
                {
                    NetworkPeer.OnConnectFailed(ConnectionFailedReason.Timeout);
                    return;
                }

                if (disconnectReason == DisconnectReason.ConnectionRejected)
                {
                    NetworkPeer.OnConnectFailed(ConnectionFailedReason.Refused);
                    return;
                }
            }

            if (_connections.TryGetValue(peer, out var connection))
            {
                TransportDisconnectReason reason = disconnectReason == DisconnectReason.Timeout ? TransportDisconnectReason.Timeout : TransportDisconnectReason.Shutdown;

                NetworkPeer.OnDisconnected(connection, TransportDisconnectReason.Shutdown);

                _connections.Remove(peer);
                _freeClients.Enqueue(connection);
            }
        }

        void IWebRTCNetEventListener.OnNetworkReceive(BaseWebRTCPeer peer, byte[] bytes, int length)
        {
            if (_connections.TryGetValue(peer, out var connection))
            {
                fixed (byte* ptr = bytes)
                {
                    _bitBuffer.SetFrom(ptr, bytes.Length, length);
                    NetworkPeer.Receive(connection, _bitBuffer);
                }
            }
        }
    }
}
