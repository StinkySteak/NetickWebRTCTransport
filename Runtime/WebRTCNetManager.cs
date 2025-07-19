using Netick;
using System.Collections.Generic;
using Unity.WebRTC;

namespace StinkySteak.N2D
{
    public class WebRTCNetManager
    {
        private NetickEngine _engine;
        private IWebRTCNetEventListener _listener;
        private SignalingServerConnectConfig _signalingServerConnectConfig;
        private UserRTCConfig _userRTCConfig;

        private SignalingWebClient _signalingWebClient;

        private string _joinCodeAllocation;

        private List<WebRTCPeer> _candidatePeers;
        private List<WebRTCPeer> _activePeers;

        private WebRTCPeer _serverConnection;
        private WebRTCPeer _serverConnectionCandidate;
        private HostAllocationService _hostAllocationService;

        public HostAllocationService HostAllocationService => _hostAllocationService;

        public void Init(NetickEngine engine, IWebRTCNetEventListener listener, SignalingServerConnectConfig signalingServerConnectConfig, UserRTCConfig userRTCConfig)
        {
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

        public void DisconnectPeer(WebRTCPeer peer)
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
            for (int i = 0; i < _activePeers.Count; i++)
            {
                WebRTCPeer client = _activePeers[i];

                client.CloseConnection();
            }

            for (int i = 0; i < _candidatePeers.Count; i++)
            {
                WebRTCPeer client = _candidatePeers[i];

                client.CloseConnection();
            }

            _activePeers.Clear();
            _candidatePeers.Clear();

            _signalingWebClient.Stop();
        }

        private void OnWebClientMessageOffer(SignalingMessageOffer message)
        {
            RTCSessionDescription sdp = message.Offer;

            WebRTCPeer candidatePeer = new WebRTCPeer();
            candidatePeer.Init(_engine, _signalingWebClient);
            candidatePeer.SetFromOfferConnectionId(message.FromConnectionId);
            candidatePeer.StartFromOffer(sdp);

            _candidatePeers.Add(candidatePeer);

            Log.Debug($"[{nameof(WebRTCNetManager)}]: Received offering from client");
        }

        public void PollUpdate()
        {
            if (_engine.IsServer)
            {
                _hostAllocationService.PollUpdate();

                for (int i = _candidatePeers.Count - 1; i >= 0; i--)
                {
                    WebRTCPeer peer = _candidatePeers[i];
                    peer.PollUpdate();

                    if (peer.IsConnectionOpen)
                    {
                        _candidatePeers.RemoveAt(i);
                        _activePeers.Add(peer);

                        peer.OnMessageReceived += OnMessageReceived;

                        _listener.OnPeerConnected(peer);
                    }
                }

                for (int i = _activePeers.Count - 1; i >= 0; i--)
                {
                    WebRTCPeer peer = _activePeers[i];
                    peer.PollUpdate();

                    if (!peer.IsConnectionOpen)
                    {
                        _activePeers.RemoveAt(i);

                        peer.OnMessageReceived -= OnMessageReceived;

                        _listener.OnPeerDisconnected(peer);
                    }
                }
            }

            if (_engine.IsClient)
            {
                if (_serverConnectionCandidate != null)
                {
                    _serverConnectionCandidate.PollUpdate();

                    if (_serverConnectionCandidate.IsConnectionOpen)
                    {
                        _serverConnection = _serverConnectionCandidate;
                        _serverConnection.OnMessageReceived += OnMessageReceived;

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

                        _listener.OnPeerDisconnected(_serverConnection);
                    }
                }
            }

            _signalingWebClient.PollUpdate();
        }

        private void OnMessageReceived(WebRTCPeer peer, byte[] bytes)
        {
            _listener.OnNetworkReceive(peer, bytes);
        }

        private void OnWebClientAsClientConnected()
        {
            _serverConnectionCandidate = new WebRTCPeer();
            _serverConnectionCandidate.Init(_engine, _signalingWebClient);
            _serverConnectionCandidate.SetToJoinCode(_joinCodeAllocation);
            _serverConnectionCandidate.StartAndOffer();

            Log.Debug($"[{nameof(WebRTCNetManager)}]: Start and Offering");
        }

        public void Connect(string joinCode)
        {
            _joinCodeAllocation = joinCode;

            _signalingWebClient.OnConnected += OnWebClientAsClientConnected;

            _signalingWebClient.Connect(_signalingServerConnectConfig);

            Log.Debug($"[{nameof(WebRTCNetManager)}]: Connecting to {_signalingServerConnectConfig.Address}:{_signalingServerConnectConfig.Port}");
        }
    }
}
