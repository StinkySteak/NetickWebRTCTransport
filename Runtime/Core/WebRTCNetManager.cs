using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class WebRTCNetManager
    {
        private NetickEngine _engine;
        private IWebRTCNetEventListener _listener;
        private SignalingServerConnectConfig _signalingServerConnectConfig;
        private UserRTCConfig _userRTCConfig;

        private SignalingWebClient _signalingWebClient;

        private string _joinCodeAllocation;

        private List<BaseWebRTCPeer> _candidatePeers;
        private List<BaseWebRTCPeer> _activePeers;

        private BaseWebRTCPeer _serverConnection;
        private BaseWebRTCPeer _serverConnectionCandidate;
        private HostAllocationService _hostAllocationService;
        private byte[] _receiveBuffer;

        public HostAllocationService HostAllocationService => _hostAllocationService;

        internal void Init(NetickEngine engine, IWebRTCNetEventListener listener, SignalingServerConnectConfig signalingServerConnectConfig, UserRTCConfig userRTCConfig)
        {
            _receiveBuffer = new byte[2048];

            _engine = engine;
            _listener = listener;
            _signalingServerConnectConfig = signalingServerConnectConfig;
            _userRTCConfig = userRTCConfig;

            _signalingWebClient = new SignalingWebClient();

            if (_engine.IsServer)
            {
                _hostAllocationService = new HostAllocationService();
                _hostAllocationService.Init(_signalingWebClient, _signalingServerConnectConfig);

                _candidatePeers = new(_engine.Config.MaxPlayers);
                _activePeers = new(_engine.Config.MaxPlayers);
            }
        }

        public void DisconnectPeer(BaseWebRTCPeer peer)
        {
            peer.CloseConnection();
        }

        public void Start()
        {
            if (_engine.IsServer)
            {
                _hostAllocationService.RequestAllocation();
                _signalingWebClient.OnMessageOffer += OnWebClientMessageOffer;
            }
        }

        public void Stop()
        {
            if (_engine.IsServer)
            {
                for (int i = 0; i < _activePeers.Count; i++)
                {
                    BaseWebRTCPeer client = _activePeers[i];

                    client.CloseConnection();
                }

                for (int i = 0; i < _candidatePeers.Count; i++)
                {
                    BaseWebRTCPeer client = _candidatePeers[i];

                    client.CloseConnection();
                }

                _activePeers.Clear();
                _candidatePeers.Clear();
            }

            _signalingWebClient.Stop();
        }

        private void OnWebClientMessageOffer(SignalingMessageOffer message)
        {
            BaseWebRTCPeer candidatePeer = ConstructWebRTCPeer();
            candidatePeer.Init(_engine, _signalingWebClient, _userRTCConfig);
            candidatePeer.SetFromOfferConnectionId(message.FromConnectionId);
            candidatePeer.StartFromOffer(message.Offer);

            _candidatePeers.Add(candidatePeer);

            Log.Info($"[{nameof(WebRTCNetManager)}]: Received offering from client");
        }

        public void PollUpdate()
        {
            if (_engine.IsServer)
            {
                _hostAllocationService.PollUpdate();

                for (int i = _candidatePeers.Count - 1; i >= 0; i--)
                {
                    BaseWebRTCPeer peer = _candidatePeers[i];
                    peer.PollUpdate();

                    if (peer.IsConnectionOpen)
                    {
                        _candidatePeers.RemoveAt(i);
                        _activePeers.Add(peer);

                        peer.OnMessageReceived += OnMessageReceived;
                        peer.OnMessageReceivedUnmanaged += OnMessageReceivedUnmanaged;

                        _listener.OnPeerConnected(peer);
                    }
                }

                for (int i = _activePeers.Count - 1; i >= 0; i--)
                {
                    BaseWebRTCPeer peer = _activePeers[i];
                    peer.PollUpdate();

                    if (!peer.IsConnectionOpen)
                    {
                        _activePeers.RemoveAt(i);

                        peer.OnMessageReceived -= OnMessageReceived;
                        peer.OnMessageReceivedUnmanaged -= OnMessageReceivedUnmanaged;

                        _listener.OnPeerDisconnected(peer, DisconnectReason.ConnectionClosed);
                    }
                }
            }

            if (_engine.IsClient)
            {
                if (_serverConnectionCandidate != null)
                {
                    _serverConnectionCandidate.PollUpdate();

                    if (_serverConnectionCandidate.IsTimedOut)
                    {
                        _listener.OnPeerDisconnected(_serverConnectionCandidate, DisconnectReason.Timeout);
                        _serverConnectionCandidate.CloseConnection();
                        return;
                    }

                    if (_serverConnectionCandidate.IsConnectionOpen)
                    {
                        _serverConnection = _serverConnectionCandidate;
                        _serverConnection.OnMessageReceived += OnMessageReceived;
                        _serverConnection.OnMessageReceivedUnmanaged += OnMessageReceivedUnmanaged;

                        _signalingWebClient.Stop();
                        _listener.OnPeerConnected(_serverConnection);

                        _serverConnectionCandidate = null;
                    }
                }

                if (_serverConnection != null)
                {
                    _serverConnection.PollUpdate();

                    if (!_serverConnection.IsConnectionOpen)
                    {
                        _serverConnection = null;

                        _listener.OnPeerDisconnected(_serverConnection, DisconnectReason.ConnectionClosed);
                    }
                }
            }

            _signalingWebClient.PollUpdate();
        }


        private void OnMessageReceivedUnmanaged(BaseWebRTCPeer peer, System.IntPtr ptr, int length)
        {
            Array.Clear(_receiveBuffer, 0, _receiveBuffer.Length);

            Marshal.Copy(ptr, _receiveBuffer, 0, length);
            _listener.OnNetworkReceive(peer, _receiveBuffer, length);
        }

        private void OnMessageReceived(BaseWebRTCPeer peer, byte[] bytes)
        {
            _listener.OnNetworkReceive(peer, bytes, bytes.Length);
        }

        private BaseWebRTCPeer ConstructWebRTCPeer()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                return new BrowserWebRTCPeer();

            return new NativeWebRTCPeer();
        }

        private void OnWebClientAsClientConnected()
        {
            _serverConnectionCandidate = ConstructWebRTCPeer();
            _serverConnectionCandidate.Init(_engine, _signalingWebClient, _userRTCConfig);
            _serverConnectionCandidate.SetToJoinCode(_joinCodeAllocation);
            _serverConnectionCandidate.StartAndOffer();

            Log.Info($"[{nameof(WebRTCNetManager)}]: Start and offering...");
        }

        public void Connect(string joinCode)
        {
            _joinCodeAllocation = joinCode;

            _signalingWebClient.OnConnected += OnWebClientAsClientConnected;
            _signalingWebClient.OnTimeout += OnWebClientTimeout;

            _signalingWebClient.Connect(_signalingServerConnectConfig);

            Log.Info($"[{nameof(WebRTCNetManager)}]: Connecting to {_signalingServerConnectConfig.Address}:{_signalingServerConnectConfig.Port}");
        }

        private void OnWebClientTimeout()
        {
            UnityEngine.Debug.Log("Error");
            _listener.OnPeerDisconnected(null, DisconnectReason.SignalingServerUnreachable);
        }
    }
}
